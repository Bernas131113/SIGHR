using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;
using SIGHR.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar a Connection String (usando "DefaultConnection" que aponta para "SIGHRdb")
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
    .AddEntityFrameworkStores<SIGHRContext>();

// 4. Configurar esquemas de autenticação adicionais
builder.Services.AddAuthentication()
    .AddCookie("AdminLoginScheme", options =>
    {
        // Caminho para sua Razor Page AdminLogin na área Identity
        options.LoginPath = "/Identity/Account/AdminLogin"; // << AJUSTADO
        options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Caminho padrão do Identity para Acesso Negado
        // Ou, se você tiver uma página customizada: options.AccessDeniedPath = "/Admin/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthentication()
    .AddCookie("CollaboratorLoginScheme", options =>
    {
        options.LoginPath = "/Identity/Account/CollaboratorPinLogin"; // Nova página de login para colaboradores
        options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Página de acesso negado para colaboradores
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Exemplo: sessão de 8 horas
        options.SlidingExpiration = true;
    });



// 5. Configurar outros serviços
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    // Convenções para Razor Pages, se necessário
    // Ex: options.Conventions.AuthorizeAreaPage("Identity", "/Account/Manage");
});

// ----- Fim da configuração de Serviços -----
var app = builder.Build();

// ----- Configurar o Pipeline de Requisições HTTP -----
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
app.UseAuthentication(); // Essencial para os esquemas de autenticação
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // Mapeia Razor Pages, incluindo as do Identity

// ----- Seeding de Dados Iniciais -----
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

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminUserForPinLoginAsync(userManager, roleManager, logger, configuration);
    }
    catch (Exception ex)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "Ocorreu um erro durante o seeding da base de dados.");
    }
}

app.Run();

// ----- Métodos de Seeding (Coloque-os no final do Program.cs ou em uma classe separada) -----
async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger<Program> logger)
{
    string[] roleNames = { "Admin", "Office", "Collaborator" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (roleResult.Succeeded) logger.LogInformation($"Role '{roleName}' criada.");
            else foreach (var error in roleResult.Errors) logger.LogError($"Erro ao criar role '{roleName}': {error.Description}");
        }
    }
}

async Task SeedAdminUserForPinLoginAsync(
    UserManager<SIGHRUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<Program> logger,
    IConfiguration configuration)
{
    string? adminUserName = configuration["SeedAdminCredentials:UserName"];
    string? adminEmail = configuration["SeedAdminCredentials:Email"];
    string? adminPIN_str = configuration["SeedAdminCredentials:PIN"];
    string? adminNomeCompleto = configuration["SeedAdminCredentials:NomeCompleto"];
    string? adminPassword = configuration["SeedAdminCredentials:Password"];

    string adminRoleName = "Admin";

    if (string.IsNullOrEmpty(adminUserName)) { adminUserName = "bernardo.alves"; logger.LogWarning("SeedAdminCredentials:UserName não configurado, usando fallback."); }
    if (string.IsNullOrEmpty(adminEmail)) { adminEmail = $"default_{adminUserName}@sighr-placeholder.com"; logger.LogWarning("SeedAdminCredentials:Email não configurado, usando fallback."); }
    if (string.IsNullOrEmpty(adminPIN_str) || !int.TryParse(adminPIN_str, out int adminPIN)) { adminPIN = 1311; logger.LogWarning("SeedAdminCredentials:PIN não configurado ou inválido, usando fallback."); }
    if (string.IsNullOrEmpty(adminNomeCompleto)) { adminNomeCompleto = "Bernardo Alves (Admin)"; }
    if (string.IsNullOrEmpty(adminPassword)) { adminPassword = Guid.NewGuid().ToString() + "XyZ789#"; logger.LogInformation("SeedAdminCredentials:Password não configurado, gerando senha dummy."); }


    var existingUser = await userManager.FindByNameAsync(adminUserName);
    if (existingUser == null)
    {
        var adminUser = new SIGHRUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true,
            NomeCompleto = adminNomeCompleto,
            PIN = adminPIN,
            Tipo = adminRoleName
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            logger.LogInformation($"Usuário '{adminUser.UserName}' (para login por PIN) criado.");
            if (await roleManager.RoleExistsAsync(adminRoleName))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
                if (addToRoleResult.Succeeded) logger.LogInformation($"Usuário '{adminUser.UserName}' adicionado ao role '{adminRoleName}'.");
                else foreach (var error in addToRoleResult.Errors) logger.LogError($"Erro ao adicionar usuário ao role: {error.Description}");
            }
            else logger.LogWarning($"Role '{adminRoleName}' não encontrada.");
        }
        else foreach (var error in result.Errors) logger.LogError($"Erro ao criar usuário '{adminUser.UserName}': {error.Description}");
    }
    else
    {
        logger.LogInformation($"Usuário '{adminUserName}' já existe. Verificando/atualizando PIN, Tipo, Role.");
        bool needsUpdate = false;
        if (existingUser.PIN != adminPIN) { existingUser.PIN = adminPIN; needsUpdate = true; }
        if (existingUser.Tipo != adminRoleName) { existingUser.Tipo = adminRoleName; needsUpdate = true; }
        if (adminNomeCompleto != null && existingUser.NomeCompleto != adminNomeCompleto) { existingUser.NomeCompleto = adminNomeCompleto; needsUpdate = true; }
        if (needsUpdate)
        {
            var updateResult = await userManager.UpdateAsync(existingUser);
            if (updateResult.Succeeded) logger.LogInformation($"Dados atualizados para '{adminUserName}'.");
            else foreach (var error in updateResult.Errors) logger.LogError($"Erro ao atualizar dados de '{adminUserName}': {error.Description}");
        }
        if (!await userManager.IsInRoleAsync(existingUser, adminRoleName) && await roleManager.RoleExistsAsync(adminRoleName))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(existingUser, adminRoleName);
            if (addToRoleResult.Succeeded) logger.LogInformation($"Usuário '{adminUserName}' (existente) adicionado ao role '{adminRoleName}'.");
            else foreach (var error in addToRoleResult.Errors) logger.LogError($"Erro ao adicionar usuário existente ao role: {error.Description}");
        }
    }
}