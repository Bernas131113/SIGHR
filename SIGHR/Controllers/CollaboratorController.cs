// Controllers/CollaboratorController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;// << Ajuste este namespace se SIGHRContext/SIGHRUser estiverem em outro lugar
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using SIGHR.Models.ViewModels; // Para CollaboratorDashboardViewModel
using Microsoft.Extensions.Logging; // Para ILogger

namespace SIGHR.Controllers
{
    [Authorize(Roles = "Admin,Collaborator,Office", AuthenticationSchemes = "AdminLoginScheme,CollaboratorLoginScheme")]
    public class CollaboratorController : Controller
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<CollaboratorController> _logger;

        public CollaboratorController(SIGHRContext context, ILogger<CollaboratorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Em vez de Challenge(), que pode levar ao login padrão do Identity,
                // se o usuário não estiver autenticado com o esquema esperado,
                // é melhor retornar Unauthorized() ou redirecionar para o login do colaborador.
                // No entanto, o [Authorize] no controller já deve cuidar disso.
                // Se chegar aqui sem userId, é um estado inesperado.
                _logger.LogWarning("Dashboard acessado sem UserID no ClaimTypes.NameIdentifier.");
                return Unauthorized("Utilizador não identificado.");
            }

            var user = await _context.Users
                                    .Include(u => u.Horarios.Where(h => h.Data.Date == DateTime.Today))
                                    .Include(u => u.Faltas.OrderByDescending(f => f.DataFalta).Take(5))
                                    .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning($"Dashboard: Utilizador com ID {userId} não encontrado no banco.");
                return NotFound("Utilizador não encontrado.");
            }

            var viewModel = new CollaboratorDashboardViewModel
            {
                NomeCompleto = user.NomeCompleto ?? user.UserName,
                HorarioDeHoje = user.Horarios.FirstOrDefault(),
                UltimasFaltas = user.Faltas.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string? userName = User.Identity?.Name ?? "Usuário desconhecido";
            await HttpContext.SignOutAsync("CollaboratorLoginScheme");
            _logger.LogInformation($"Usuário '{userName}' deslogado do CollaboratorLoginScheme.");

            // ***** CORREÇÃO PRINCIPAL AQUI *****
            // Redireciona para a Razor Page de login do colaborador na área Identity
            return RedirectToPage("/Account/CollaboratorPinLogin", new { area = "Identity" });
        }
    }
}