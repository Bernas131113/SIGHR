// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Para ILogger
using System.Threading.Tasks;

namespace SIGHR.Controllers // Certifique-se que este namespace está correto
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminLoginScheme")] // Protege todo o controller
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        // GET: /Admin/ ou /Admin/Index
        public IActionResult Index()
        {
            _logger.LogInformation("Acessando o Painel do Administrador (AdminController/Index).");
            return View(); // Retorna Views/Admin/Index.cshtml
        }

        // Action de Logout para o AdminLoginScheme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string? userName = User.Identity?.Name ?? "Usuário desconhecido";
            await HttpContext.SignOutAsync("AdminLoginScheme");
            _logger.LogInformation($"Usuário '{userName}' deslogado do AdminLoginScheme. Redirecionando para a página de login do admin.");

            // Redireciona para a Razor Page de login do admin na área Identity
            // Isso assume que sua AdminLogin.cshtml está em Areas/Identity/Pages/Account/AdminLogin.cshtml
            // e que o LoginPath no Program.cs para AdminLoginScheme é "/Identity/Account/AdminLogin"
            return RedirectToPage("/Account/AdminLogin", new { area = "Identity" });
        }
    }
}