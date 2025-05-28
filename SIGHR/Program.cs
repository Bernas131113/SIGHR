using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;
using SIGHR.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar a Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Please define it in appsettings.json.");

// 2. Adicionar o DbContext (SIGHRContext)
builder.Services.AddDbContext<SIGHRContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Configurar o ASP.NET Core Identity (Isso configura o IdentityConstants.ApplicationScheme)
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
    .AddRoles<IdentityRole>() // Habilita o uso de Funções (Roles)
    .AddEntityFrameworkStores<SIGHRContext>(); // Diz ao Identity para usar SEU SIGHRContext

// 4. Configurar a Autenticação e os esquemas de Cookie Adicionais
//    AddDefaultIdentity já chama AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme)
//    Então, aqui configuramos o DefaultChallengeScheme e adicionamos os outros esquemas.
builder.Services.AddAuthentication(options =>
{
    // Define o esquema que será usado para desafiar o usuário (redirecionar para login)
    // quando a autorização falha e nenhum esquema específico é satisfeito pelo usuário.
    // Se um colaborador anônimo tenta acessar FaltasController, ele será enviado para /Identity/Account/Login.
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme; // Usado para logins externos, etc.
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme; // Qual esquema usar para autenticar por padrão se não especificado
})
    .AddCookie("AdminLoginScheme", options => // Seu esquema para Admin PIN login
    {
        options.LoginPath = "/Identity/Account/AdminLogin"; // Rota da sua Razor Page AdminLogin
        options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Página de acesso negado
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    .AddCookie("CollaboratorLoginScheme", options => // Seu esquema para Colaborador PIN login
    {
        options.LoginPath = "/Identity/Account/CollaboratorPinLogin"; // Rota da sua Razor Page CollaboratorPinLogin
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
// O cookie para IdentityConstants.ApplicationScheme já foi configurado por AddDefaultIdentity.
// Se você precisar customizar as opções dele (como o LoginPath se não for o padrão),
// você pode usar builder.Services.ConfigureApplicationCookie(options => { ... }); APÓS AddDefaultIdentity.
// Exemplo:
// builder.Services.ConfigureApplicationCookie(options =>
// {
//     options.LoginPath = "/Identity/Account/Login"; // Caminho padrão, mas pode ser customizado
// });


// 5. Configurar outros serviços
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    // Convenções para Razor Pages, se necessário
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

// IMPORTANTE: Ordem correta do Middleware
app.UseAuthentication(); // Processa os cookies de autenticação e estabelece a identidade do usuário
app.UseAuthorization();  // Verifica se o usuário autenticado tem permissão para acessar o recurso

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // Mapeia Razor Pages

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
        // Considere adicionar um método para semear usuários Colaboradores e Office se necessário
        // await SeedCollaboratorUserAsync(userManager, roleManager, logger, configuration);
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
    string? adminPassword = configuration["SeedAdminCredentials:Password"]; // Senha para o Identity

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
            logger.LogInformation($"Usuário '{adminUser.UserName}' (Admin) criado.");
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
        logger.LogInformation($"Usuário '{adminUserName}' (Admin) já existe. Verificando/atualizando.");
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
// Você precisaria de um método similar para semear usuários Colaboradores e Office,
// garantindo que eles sejam adicionados aos roles "Collaborator" e "Office" respectivamente.