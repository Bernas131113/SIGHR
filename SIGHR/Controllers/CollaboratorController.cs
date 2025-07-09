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
    // A política "CollaboratorAccess" permite acesso a Admin e Collaborator.
    [Authorize(Policy = "CollaboratorAccessUI")]
    public class CollaboratorController : Controller
    {
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly ILogger<CollaboratorController> _logger;

        public CollaboratorController(SIGHRContext context, UserManager<SIGHRUser> userManager, ILogger<CollaboratorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Painel do Colaborador";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Utilizador não identificado.");

            var user = await _context.Users
                .Include(u => u.Horarios.Where(h => h.Data.Date == DateTime.Today))
                .Include(u => u.Faltas.OrderByDescending(f => f.DataFalta).Take(5))
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound("Utilizador não encontrado.");

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
            await HttpContext.SignOutAsync("CollaboratorLoginScheme");
            return RedirectToPage("/Account/CollaboratorPinLogin", new { area = "Identity" });
        }
    }
}