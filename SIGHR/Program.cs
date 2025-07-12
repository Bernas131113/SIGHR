using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SIGHR.Areas.Identity.Data;
using SIGHR.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//
// Bloco 1: Configuração da Base de Dados
//
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não foi encontrada em appsettings.json.");
builder.Services.AddDbContext<SIGHRContext>(options =>
    options.UseSqlServer(connectionString));

//
// Bloco 2: Configuração do ASP.NET Core Identity
// Define as regras para utilizadores e palavras-passe e associa o Identity ao nosso DbContext.
//
builder.Services.AddDefaultIdentity<SIGHRUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Não exige confirmação de email para login.
    options.User.RequireUniqueEmail = true;         // Cada email deve ser único.
    options.Password.RequiredLength = 8;            // Comprimento mínimo da palavra-passe.
    options.Password.RequireNonAlphanumeric = false; // Não exige caracteres especiais.
})
    .AddRoles<IdentityRole>() // Ativa a gestão de Funções (Roles).
    .AddEntityFrameworkStores<SIGHRContext>();

//
// Bloco 3: Configuração da Autenticação (Como os utilizadores provam quem são)
// Define os diferentes "esquemas" de login (cookies para a UI, JWT para a API).
//
builder.Services.AddAuthentication()
   // Esquema de cookie para o login de Administradores na UI.
   .AddCookie("AdminLoginScheme", options =>
   {
       options.LoginPath = "/Identity/Account/AdminLogin";
       options.AccessDeniedPath = "/Identity/Account/AccessDenied";
       options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
   })
   // Esquema de cookie para o login de Colaboradores na UI.
   .AddCookie("CollaboratorLoginScheme", options =>
   {
       options.LoginPath = "/Identity/Account/CollaboratorPinLogin";
       options.AccessDeniedPath = "/Identity/Account/AccessDenied";
       options.ExpireTimeSpan = TimeSpan.FromHours(8);
   })
   // Esquema de autenticação JWT (Bearer Token) para as APIs.
   .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
   {
       var jwtSettings = builder.Configuration.GetSection("Jwt");
       var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("A Chave JWT não foi encontrada."));
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = jwtSettings["Issuer"],
           ValidAudience = jwtSettings["Audience"],
           IssuerSigningKey = new SymmetricSecurityKey(key),
           ClockSkew = TimeSpan.Zero // Remove a margem de tempo padrão na validação da expiração.
       };
   });

//
// Bloco 4: Configuração da Autorização (O que os utilizadores podem fazer)
// Define políticas de acesso que combinam funções (roles) e esquemas de autenticação.
//
builder.Services.AddAuthorization(options => {
    var cookieSchemes = new[] { IdentityConstants.ApplicationScheme, "AdminLoginScheme", "CollaboratorLoginScheme" };
    var jwtScheme = new[] { JwtBearerDefaults.AuthenticationScheme };

    // Políticas para a Interface do Utilizador (UI), baseadas em cookies.
    options.AddPolicy("AdminAccessUI", p => p.RequireRole("Admin").AddAuthenticationSchemes(cookieSchemes).RequireAuthenticatedUser());
    options.AddPolicy("CollaboratorAccessUI", p => p.RequireRole("Admin", "Collaborator").AddAuthenticationSchemes(cookieSchemes).RequireAuthenticatedUser());

    // Políticas para as APIs, baseadas em tokens JWT.
    options.AddPolicy("AdminAccessApi", p => p.RequireRole("Admin").AddAuthenticationSchemes(jwtScheme).RequireAuthenticatedUser());
    options.AddPolicy("CollaboratorAccessApi", p => p.RequireRole("Admin", "Collaborator").AddAuthenticationSchemes(jwtScheme).RequireAuthenticatedUser());

    // Política híbrida que permite acesso a uma API tanto por cookie (UI) como por JWT (externo).
    options.AddPolicy("AdminGeneralApiAccess", p => p.RequireRole("Admin").AddAuthenticationSchemes(cookieSchemes).AddAuthenticationSchemes(jwtScheme).RequireAuthenticatedUser());
});

//
// Bloco 5: Configuração de Outros Serviços da Aplicação
//
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddRazorPages();
builder.Services.AddScoped<TokenService>(); // Regista o nosso serviço de geração de tokens.
builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger para documentação da API.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API do SIGHR",
        Version = "v1",
        Description = "API para o Sistema Integrado de Gestão de Recursos Humanos"
    });

    // Configura o Swagger para ler os comentários XML do código e exibi-los na documentação.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

//
// Bloco 6: Pipeline de Middleware HTTP
// Define a ordem em que os pedidos são processados.
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Essencial: verifica quem é o utilizador.
app.UseAuthorization();  // Essencial: verifica o que o utilizador pode fazer.

// Mapeia as rotas para os controladores e páginas.
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapControllers();

//
// Bloco 7: Seeding da Base de Dados (Criação de Dados Iniciais)
// Executa na primeira vez que a aplicação arranca para popular dados essenciais.
//
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


//
// Bloco 8: Métodos de Seeding
//


// Cria as funções (Roles) "Admin" e "Collaborator" se ainda não existirem.

async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger<Program> logger)
{
    string[] roleNames = { "Admin", "Collaborator" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded) logger.LogInformation("Função '{RoleName}' criada.", roleName);
            else foreach (var error in result.Errors) logger.LogError("Erro ao criar a função '{RoleName}': {ErrorDescription}", roleName, error.Description);
        }
    }
}


// Cria um utilizador administrador inicial com um PIN codificado, lendo os dados de appsettings.json.

async Task SeedAdminUserWithHashedPinAsync(UserManager<SIGHRUser> userManager, RoleManager<IdentityRole> roleManager, IPasswordHasher<SIGHRUser> pinHasher, ILogger<Program> logger, IConfiguration configuration)
{
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


// Cria um utilizador de teste específico para a API, com uma palavra-passe conhecida.

async Task SeedApiTestUserAsync(UserManager<SIGHRUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<Program> logger)
{
    string testUserName = "apitest";
    string testEmail = "api@test.com";
    string testPassword = "PasswordApi123!";
    string testRoleName = "Admin";

    var existingUser = await userManager.FindByNameAsync(testUserName);
    if (existingUser == null)
    {
        var apiUser = new SIGHRUser
        {
            UserName = testUserName,
            Email = testEmail,
            EmailConfirmed = true,
            NomeCompleto = "Utilizador de Teste API",
            Tipo = testRoleName,
            PinnedHash = null
        };

        var result = await userManager.CreateAsync(apiUser, testPassword);
        if (result.Succeeded)
        {
            logger.LogInformation("Utilizador de teste de API '{UserName}' criado com sucesso.", apiUser.UserName);
            if (await roleManager.RoleExistsAsync(testRoleName))
            {
                await userManager.AddToRoleAsync(apiUser, testRoleName);
                logger.LogInformation("Utilizador '{UserName}' adicionado à função '{RoleName}'.", apiUser.UserName, testRoleName);
            }
        }
        else
        {
            foreach (var error in result.Errors)
            {
                logger.LogError("Erro ao criar o utilizador de teste de API: {ErrorDescription}", error.Description);
            }
        }
    }
}