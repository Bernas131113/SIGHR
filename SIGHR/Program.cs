using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SIGHR.Areas.Identity.Data;
using SIGHR.Services;   // Onde TokenService está definido
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar a Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");

// 2. Adicionar o DbContext
builder.Services.AddDbContext<SIGHRContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Configurar ASP.NET Core Identity
builder.Services.AddDefaultIdentity<SIGHRUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SIGHRContext>();

// 4. Configurar Autenticação e Esquemas de Cookie
builder.Services.AddAuthentication()
   .AddCookie("AdminLoginScheme", options =>
   {
       options.LoginPath = "/Identity/Account/AdminLogin";
       options.AccessDeniedPath = "/Identity/Account/AccessDenied";
       options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
   })
   .AddCookie("CollaboratorLoginScheme", options =>
   {
       options.LoginPath = "/Identity/Account/CollaboratorPinLogin";
       options.AccessDeniedPath = "/Identity/Account/AccessDenied";
       options.ExpireTimeSpan = TimeSpan.FromHours(8);
   })
   // Adiciona autenticação JWT para API
   .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
   {
       var jwtSettings = builder.Configuration.GetSection("Jwt");
       var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not found."));
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = jwtSettings["Issuer"],
           ValidAudience = jwtSettings["Audience"],
           IssuerSigningKey = new SymmetricSecurityKey(key),
           ClockSkew = TimeSpan.Zero
       };
   });

// 5. Configurar Políticas de Autorização (Simplificado para Admin e Colaborador)
builder.Services.AddAuthorization(options => {
    var cookieSchemes = new[] { IdentityConstants.ApplicationScheme, "AdminLoginScheme", "CollaboratorLoginScheme" };
    var jwtScheme = new[] { JwtBearerDefaults.AuthenticationScheme };
    options.AddPolicy("AdminAccessUI", p => p.RequireRole("Admin").AddAuthenticationSchemes(cookieSchemes).RequireAuthenticatedUser());
    options.AddPolicy("CollaboratorAccessUI", p => p.RequireRole("Admin", "Collaborator").AddAuthenticationSchemes(cookieSchemes).RequireAuthenticatedUser());
    options.AddPolicy("AdminAccessApi", p => p.RequireRole("Admin").AddAuthenticationSchemes(jwtScheme).RequireAuthenticatedUser());
    options.AddPolicy("CollaboratorAccessApi", p => p.RequireRole("Admin", "Collaborator").AddAuthenticationSchemes(jwtScheme).RequireAuthenticatedUser());
    // Adicionada a política híbrida para APIs chamadas da UI
    options.AddPolicy("AdminGeneralApiAccess", p => p.RequireRole("Admin").AddAuthenticationSchemes(cookieSchemes).AddAuthenticationSchemes(jwtScheme).RequireAuthenticatedUser());
});

// 6. Outros Serviços
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddRazorPages();
builder.Services.AddScoped<TokenService>();
// Program.cs
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de SIGHR",
        Version = "v1",
        Description = "API para SIGHR"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

else { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapControllers();

// ----- Seeding de Dados -----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<SIGHRUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var pinHasher = services.GetRequiredService<IPasswordHasher<SIGHRUser>>();

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminUserWithHashedPinAsync(userManager, roleManager, pinHasher, logger, configuration);
        await SeedApiTestUserAsync(userManager, roleManager, logger);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro durante o seeding da base de dados.");
    }
}
app.Run();

// ----- Métodos de Seeding -----
async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger<Program> logger)
{
    // Removido "Office" da lista
    string[] roleNames = { "Admin", "Collaborator" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded) logger.LogInformation("Role '{RoleName}' criada.", roleName);
            else foreach (var error in result.Errors) logger.LogError("Erro ao criar role '{RoleName}': {ErrorDescription}", roleName, error.Description);
        }
    }
}

async Task SeedAdminUserWithHashedPinAsync(UserManager<SIGHRUser> userManager, RoleManager<IdentityRole> roleManager, IPasswordHasher<SIGHRUser> pinHasher, ILogger<Program> logger, IConfiguration configuration)
{
    // ... Este método permanece como está ...
    string configUserName = configuration["SeedAdminCredentials:UserName"] ?? "bernardo.alves";
    string configEmail = configuration["SeedAdminCredentials:Email"] ?? $"admin_{configUserName}@sighr.com";
    if (!int.TryParse(configuration["SeedAdminCredentials:PIN"] ?? "1311", out int adminPIN)) adminPIN = 1311;
    string configNomeCompleto = configuration["SeedAdminCredentials:NomeCompleto"] ?? "Bernardo Alves (Admin)";
    string configPassword = configuration["SeedAdminCredentials:Password"] ?? Guid.NewGuid().ToString() + "P@ss1!";
    string adminRoleName = "Admin";
    var existingUser = await userManager.FindByNameAsync(configUserName);
    if (existingUser == null)
    {
        var user = new SIGHRUser { UserName = configUserName, Email = configEmail, EmailConfirmed = true, NomeCompleto = configNomeCompleto, Tipo = adminRoleName, PinnedHash = pinHasher.HashPassword(null!, adminPIN.ToString()) };
        var result = await userManager.CreateAsync(user, configPassword);
        if (result.Succeeded) await userManager.AddToRoleAsync(user, adminRoleName);
    }
    else { if (!await userManager.IsInRoleAsync(existingUser, adminRoleName)) await userManager.AddToRoleAsync(existingUser, adminRoleName); }
}

// Adicione este método ao final do seu Program.cs, junto com os outros
async Task SeedApiTestUserAsync(UserManager<SIGHRUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<Program> logger)
{
    string testUserName = "apitest";
    string testEmail = "api@test.com";
    string testPassword = "PasswordApi123!"; // <<< Uma senha que você conhece!
    string testRoleName = "Admin"; // Para que este usuário possa testar endpoints de admin

    var existingUser = await userManager.FindByNameAsync(testUserName);
    if (existingUser == null)
    {
        var apiUser = new SIGHRUser
        {
            UserName = testUserName,
            Email = testEmail,
            EmailConfirmed = true,
            NomeCompleto = "Usuário de Teste API",
            Tipo = testRoleName,
            PinnedHash = null // Este usuário não precisa de PIN de login
        };

        var result = await userManager.CreateAsync(apiUser, testPassword);
        if (result.Succeeded)
        {
            logger.LogInformation("Usuário de teste de API '{UserName}' criado com sucesso.", apiUser.UserName);
            if (await roleManager.RoleExistsAsync(testRoleName))
            {
                await userManager.AddToRoleAsync(apiUser, testRoleName);
                logger.LogInformation("Usuário '{UserName}' adicionado ao role '{RoleName}'.", apiUser.UserName, testRoleName);
            }
        }
        else
        {
            foreach (var error in result.Errors)
            {
                logger.LogError("Erro ao criar usuário de teste de API: {ErrorDescription}", error.Description);
            }
        }
    }
}