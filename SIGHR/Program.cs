using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration; // Para IConfiguration

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar a Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Please define it in appsettings.json.");

// 2. Adicionar o DbContext (SIGHRContext)
builder.Services.AddDbContext<SIGHRContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Configurar o ASP.NET Core Identity
builder.Services.AddDefaultIdentity<SIGHRUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    // IPasswordHasher<SIGHRUser> é registrado por padrão pelo AddDefaultIdentity
    .AddEntityFrameworkStores<SIGHRContext>();

// 4. Configurar esquemas de autenticação adicionais
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
})
    .AddCookie("AdminLoginScheme", options =>
    {
        options.LoginPath = "/Identity/Account/AdminLogin";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    .AddCookie("CollaboratorLoginScheme", options =>
    {
        options.LoginPath = "/Identity/Account/CollaboratorPinLogin";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// 5. Configurar outros serviços
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<SIGHRUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var pinHasher = services.GetRequiredService<IPasswordHasher<SIGHRUser>>(); // Obter o hasher

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminUserWithHashedPinAsync(userManager, roleManager, pinHasher, logger, configuration);
    }
    catch (Exception ex)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "Ocorreu um erro durante o seeding da base de dados.");
    }
}

app.Run();

// ----- Métodos de Seeding -----
async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger<Program> logger)
{
    string[] roleNames = { "Admin", "Office", "Collaborator" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (roleResult.Succeeded) logger.LogInformation("Role '{RoleName}' criada.", roleName);
            else foreach (var error in roleResult.Errors) logger.LogError("Erro ao criar role '{RoleName}': {ErrorDescription}", roleName, error.Description);
        }
    }
}

async Task SeedAdminUserWithHashedPinAsync(
    UserManager<SIGHRUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IPasswordHasher<SIGHRUser> pinHasher,
    ILogger<Program> logger,
    IConfiguration configuration)
{
    string? configUserName = configuration["SeedAdminCredentials:UserName"];
    string? configEmail = configuration["SeedAdminCredentials:Email"];
    string? configPIN_str = configuration["SeedAdminCredentials:PIN"];
    string? configNomeCompleto = configuration["SeedAdminCredentials:NomeCompleto"];
    string? configPasswordForIdentity = configuration["SeedAdminCredentials:Password"]; // Para senha do Identity

    string adminRoleName = "Admin";

    // Validação e fallbacks
    if (string.IsNullOrEmpty(configUserName)) { configUserName = "bernardo.alves"; logger.LogWarning("SeedAdminCredentials:UserName não configurado, usando fallback 'bernardo.alves'."); }
    if (string.IsNullOrEmpty(configEmail)) { configEmail = $"default_{configUserName}@sighr-placeholder.com"; logger.LogWarning("SeedAdminCredentials:Email não configurado, usando fallback para {UserEmail}.", configEmail); }
    if (string.IsNullOrEmpty(configPIN_str) || !int.TryParse(configPIN_str, out int adminPIN_int)) { adminPIN_int = 1311; logger.LogWarning("SeedAdminCredentials:PIN não configurado ou inválido, usando fallback 1311."); }
    if (string.IsNullOrEmpty(configNomeCompleto)) { configNomeCompleto = "Bernardo Alves (Admin)"; }
    if (string.IsNullOrEmpty(configPasswordForIdentity)) { configPasswordForIdentity = Guid.NewGuid().ToString() + "XyZ789#"; logger.LogInformation("SeedAdminCredentials:Password não configurado para Identity. Gerando uma senha dummy forte."); }

    var existingUser = await userManager.FindByNameAsync(configUserName);

    if (existingUser == null)
    {
        var adminUser = new SIGHRUser
        {
            UserName = configUserName,
            Email = configEmail,
            EmailConfirmed = true,
            NomeCompleto = configNomeCompleto,
            Tipo = adminRoleName
            // PinnedHash será definido abaixo
        };
        adminUser.PinnedHash = pinHasher.HashPassword(adminUser, adminPIN_int.ToString());

        var result = await userManager.CreateAsync(adminUser, configPasswordForIdentity); // Senha dummy para Identity
        if (result.Succeeded)
        {
            logger.LogInformation("Usuário '{UserName}' (Admin) criado com PIN hasheado.", adminUser.UserName);
            if (await roleManager.RoleExistsAsync(adminRoleName))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
                if (addToRoleResult.Succeeded) logger.LogInformation("Usuário '{UserName}' adicionado ao role '{AdminRoleName}'.", adminUser.UserName, adminRoleName);
                else foreach (var error in addToRoleResult.Errors) logger.LogError("Erro ao adicionar usuário '{UserName}' ao role '{AdminRoleName}': {ErrorDescription}", adminUser.UserName, adminRoleName, error.Description);
            }
            else logger.LogWarning("Role '{AdminRoleName}' não encontrada.", adminRoleName);
        }
        else foreach (var error in result.Errors) logger.LogError("Erro ao criar usuário '{UserName}': {ErrorDescription}", adminUser.UserName, error.Description);
    }
    else // Usuário existente
    {
        logger.LogInformation("Usuário '{UserName}' (Admin) já existe. Verificando/atualizando PinnedHash, Tipo, Role.", configUserName);
        bool needsUpdate = false;
        var currentPinVerification = PasswordVerificationResult.Failed;
        if (!string.IsNullOrEmpty(existingUser.PinnedHash))
        {
            currentPinVerification = pinHasher.VerifyHashedPassword(existingUser, existingUser.PinnedHash, adminPIN_int.ToString());
        }

        if (currentPinVerification == PasswordVerificationResult.Failed)
        {
            existingUser.PinnedHash = pinHasher.HashPassword(existingUser, adminPIN_int.ToString());
            needsUpdate = true;
            logger.LogInformation("PinnedHash atualizado para o usuário '{UserName}'.", configUserName);
        }
        else if (currentPinVerification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            existingUser.PinnedHash = pinHasher.HashPassword(existingUser, adminPIN_int.ToString()); // Re-hash com o algoritmo mais recente
            needsUpdate = true;
            logger.LogInformation("PinnedHash re-hasheado para o usuário '{UserName}' devido a algoritmo atualizado.", configUserName);
        }


        if (existingUser.Tipo != adminRoleName) { existingUser.Tipo = adminRoleName; needsUpdate = true; }
        if (configNomeCompleto != null && existingUser.NomeCompleto != configNomeCompleto) { existingUser.NomeCompleto = configNomeCompleto; needsUpdate = true; }

        if (needsUpdate)
        {
            var updateResult = await userManager.UpdateAsync(existingUser);
            if (updateResult.Succeeded) logger.LogInformation("Dados atualizados para '{UserName}'.", configUserName);
            else foreach (var error in updateResult.Errors) logger.LogError("Erro ao atualizar dados de '{UserName}': {ErrorDescription}", configUserName, error.Description);
        }
        if (!await userManager.IsInRoleAsync(existingUser, adminRoleName) && await roleManager.RoleExistsAsync(adminRoleName))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(existingUser, adminRoleName);
            if (addToRoleResult.Succeeded) logger.LogInformation("Usuário '{UserName}' (existente) adicionado ao role '{AdminRoleName}'.", configUserName, adminRoleName);
            else foreach (var error in addToRoleResult.Errors) logger.LogError("Erro ao adicionar usuário '{UserName}' (existente) ao role '{AdminRoleName}': {ErrorDescription}", configUserName, adminRoleName, error.Description);
        }
    }
}