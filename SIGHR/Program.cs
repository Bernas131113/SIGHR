using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data; // Certifique-se que este é o namespace correto
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;


var builder = WebApplication.CreateBuilder(args);

// 1. Configurar a Connection String para UMA ÚNICA BASE DE DADOS
//    (Assumindo que você escolheu "DefaultConnection" como sua principal e ela aponta para "SIGHRdb")
var connectionString = builder.Configuration.GetConnectionString("SIGHRContextConnection")
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
        options.LoginPath = "/Admin/Login"; // Sua Razor Page de login de admin
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// 5. Configurar outros serviços
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ----- Seeding de Dados Iniciais -----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<SIGHRUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>(); // Obter a factory
        var logger = loggerFactory.CreateLogger<Program>(); // Criar logger para Program
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

// ----- Métodos de Seeding -----
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
    // Ler credenciais da configuração como string? para evitar aviso inicial
    string? configUserName = configuration["SeedAdminCredentials:UserName"];
    string? configEmail = configuration["SeedAdminCredentials:Email"];
    string? configPIN_str = configuration["SeedAdminCredentials:PIN"];
    string? configNomeCompleto = configuration["SeedAdminCredentials:NomeCompleto"];
    string? configPassword = configuration["SeedAdminCredentials:Password"]; // Senha para o Identity

    string adminRoleName = "Admin";

    // Validar se as configurações essenciais foram encontradas e fornecer fallbacks seguros se necessário
    if (string.IsNullOrEmpty(configUserName))
    {
        logger.LogWarning("SeedAdminCredentials:UserName não configurado. Usando 'bernardo.alves' como fallback.");
        configUserName = "bernardo.alves";
    }
    if (string.IsNullOrEmpty(configEmail))
    {
        logger.LogWarning($"SeedAdminCredentials:Email não configurado. Usando 'default_{configUserName}@sighr-placeholder.com' como fallback.");
        configEmail = $"default_{configUserName}@sighr-placeholder.com";
    }
    if (string.IsNullOrEmpty(configPIN_str) || !int.TryParse(configPIN_str, out int parsedPIN))
    {
        logger.LogWarning($"SeedAdminCredentials:PIN não configurado ou inválido. Usando '1311' como fallback.");
        parsedPIN = 1311;
    }
    // Se a senha não estiver configurada, geramos uma dummy forte.
    if (string.IsNullOrEmpty(configPassword))
    {
        configPassword = Guid.NewGuid().ToString() + "XyZ789#"; // Senha dummy forte gerada
        logger.LogInformation("SeedAdminCredentials:Password não configurada. Gerando uma senha dummy para o usuário admin do Identity.");
    }

    // Agora as variáveis que usamos para criar o usuário são garantidas como não nulas (exceto NomeCompleto e Tipo que podem ser)
    string adminUserName = configUserName; // Já validado ou com fallback
    string adminEmail = configEmail;       // Já validado ou com fallback
    int adminPIN = parsedPIN;              // Já validado ou com fallback
    string? adminNomeCompleto = configNomeCompleto; // Pode ser nulo, SIGHRUser.NomeCompleto também é string?
    string adminPasswordForIdentity = configPassword; // Já garantido como não nulo


    var existingUser = await userManager.FindByNameAsync(adminUserName);

    if (existingUser == null)
    {
        var adminUser = new SIGHRUser
        {
            UserName = adminUserName, // string
            Email = adminEmail,       // string
            EmailConfirmed = true,
            NomeCompleto = adminNomeCompleto, // string?
            PIN = adminPIN,                   // int
            Tipo = adminRoleName              // string (role name não deve ser nulo)
        };

        var result = await userManager.CreateAsync(adminUser, adminPasswordForIdentity); // Passa a senha garantida como não nula
        if (result.Succeeded)
        {
            logger.LogInformation($"Usuário '{adminUser.UserName}' (para login por PIN) criado com sucesso.");
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
        logger.LogInformation($"Usuário '{adminUserName}' (para login por PIN) já existe. Verificando/atualizando.");
        bool needsUpdate = false;
        if (existingUser.PIN != adminPIN) { existingUser.PIN = adminPIN; needsUpdate = true; }
        if (existingUser.Tipo != adminRoleName) { existingUser.Tipo = adminRoleName; needsUpdate = true; }
        // Se adminNomeCompleto da config é não nulo E diferente do existente, atualiza.
        // Se for nulo na config mas existente no BD, não limpa (a menos que essa seja a intenção).
        if (adminNomeCompleto != null && existingUser.NomeCompleto != adminNomeCompleto)
        {
            existingUser.NomeCompleto = adminNomeCompleto;
            needsUpdate = true;
        }
        else if (adminNomeCompleto == null && existingUser.NomeCompleto != null) // Se quiser limpar se não estiver na config
        {
            // existingUser.NomeCompleto = null;
            // needsUpdate = true;
        }


        if (needsUpdate)
        {
            var updateResult = await userManager.UpdateAsync(existingUser);
            if (updateResult.Succeeded) logger.LogInformation($"Dados atualizados para o usuário '{adminUserName}'.");
            else foreach (var error in updateResult.Errors) logger.LogError($"Erro ao atualizar dados: {error.Description}");
        }

        if (!await userManager.IsInRoleAsync(existingUser, adminRoleName))
        {
            if (await roleManager.RoleExistsAsync(adminRoleName))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(existingUser, adminRoleName);
                if (addToRoleResult.Succeeded) logger.LogInformation($"Usuário '{adminUserName}' (existente) adicionado ao role '{adminRoleName}'.");
                else foreach (var error in addToRoleResult.Errors) logger.LogError($"Erro ao adicionar usuário existente ao role: {error.Description}");
            }
            else logger.LogWarning($"Role '{adminRoleName}' não encontrada para usuário existente.");
        }
    }
}