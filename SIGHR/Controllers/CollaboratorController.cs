// Controllers/CollaboratorController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Models.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Controllers
{
    /// <summary>
    /// Controlador para a área do colaborador.
    /// Acesso restrito a utilizadores com as funções "Admin" ou "Collaborator",
    /// definido pela política de autorização "CollaboratorAccessUI".
    /// </summary>
    [Authorize(Policy = "CollaboratorAccessUI")]
    public class CollaboratorController : Controller
    {
        //
        // Bloco: Injeção de Dependências
        //
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly ILogger<CollaboratorController> _logger;

        public CollaboratorController(SIGHRContext context, UserManager<SIGHRUser> userManager, ILogger<CollaboratorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        //
        // Bloco: Actions do Controlador
        //

        /// <summary>
        /// Action para exibir o dashboard principal do colaborador.
        /// Reúne informações como o registo de ponto do dia e as últimas faltas.
        /// </summary>
        /// <returns>A View 'Dashboard' com os dados do utilizador autenticado.</returns>
        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Painel do Colaborador";

            // Obtém o ID do utilizador autenticado a partir das suas "claims".
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Tentativa de acesso ao Dashboard sem ID de utilizador na claim.");
                return Unauthorized("Utilizador não identificado.");
            }

            // Procura o utilizador na base de dados e inclui os dados relacionados necessários.
            // O '.Include' otimiza a consulta, trazendo os dados de ponto e faltas numa só ida à base de dados.
            var user = await _context.Users
                .Include(u => u.Horarios.Where(h => h.Data.Date == DateTime.Today)) // Apenas o ponto de hoje.
                .Include(u => u.Faltas.OrderByDescending(f => f.DataFalta).Take(5)) // Apenas as últimas 5 faltas.
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogError("Utilizador com ID {UserId} não encontrado na base de dados.", userId);
                return NotFound("Utilizador não encontrado.");
            }

            // Cria o ViewModel com os dados a serem apresentados na página.
            var viewModel = new CollaboratorDashboardViewModel
            {
                NomeCompleto = user.NomeCompleto ?? user.UserName,
                HorarioDeHoje = user.Horarios.FirstOrDefault(), // Será nulo se não houver registo para hoje.
                UltimasFaltas = user.Faltas.ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Action para realizar o logout do colaborador.
        /// Remove o cookie de autenticação do esquema "CollaboratorLoginScheme".
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CollaboratorLoginScheme");
            return RedirectToPage("/Account/CollaboratorPinLogin", new { area = "Identity" });
        }
    }
}