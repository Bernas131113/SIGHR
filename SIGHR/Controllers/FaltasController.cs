// Controllers/FaltasController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Para métodos de extensão do EF Core               // Para SIGHRContext e SIGHRUser
using SIGHR.Models;              // Para a entidade Falta
using SIGHR.Models.ViewModels;   // Para FaltaViewModel e FaltaComUserNameViewModel
using System;
using System.Linq;
using System.Security.Claims;    // Para ClaimsPrincipal, FindFirstValue, ClaimTypes
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Controllers
{
    [Authorize(Roles = "Admin,Office,Collaborator", AuthenticationSchemes = "Identity.Application,AdminLoginScheme,CollaboratorLoginScheme")]
    public class FaltasController : Controller
    {
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly ILogger<FaltasController> _logger;

        public FaltasController(SIGHRContext context, UserManager<SIGHRUser> userManager, ILogger<FaltasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Faltas/Registar
        public IActionResult Registar()
        {
            // Assumindo que FaltaViewModel.Motivo foi ajustado para ter um inicializador padrão (string.Empty)
            // ou você removeu o modificador 'required' dele.
            var model = new FaltaViewModel
            {
                DataFalta = DateTime.Today
            };
            return View(model);
        }

        // POST: Faltas/Registar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registar(FaltaViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Agora deve funcionar
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Tentativa de registar falta sem ID de usuário (não autenticado).");
                return Unauthorized("Utilizador não autenticado ou não encontrado.");
            }

            if (model.Inicio >= model.Fim)
            {
                ModelState.AddModelError(nameof(model.Fim), "A hora de fim deve ser posterior à hora de início.");
            }

            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.FindByIdAsync(userId);
                string userNameForLog = currentUser?.UserName ?? "UsuárioDesconhecido";

                var falta = new Falta
                {
                    UtilizadorId = userId,
                    Data = DateTime.Now,
                    DataFalta = model.DataFalta,
                    Inicio = model.Inicio,
                    Fim = model.Fim,
                    Motivo = model.Motivo
                };

                _context.Faltas.Add(falta);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Falta registrada com sucesso para o usuário '{userNameForLog}' (ID: {userId}).");
                TempData["SuccessMessage"] = "Falta registrada com sucesso!";
                return RedirectToAction(nameof(MinhasFaltas));
            }

            _logger.LogWarning($"Falha na validação ao tentar registar falta para o usuário ID: {userId}. Model Erros: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            return View(model);
        }

        // GET: Faltas/MinhasFaltas
        public async Task<IActionResult> MinhasFaltas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Agora deve funcionar
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            var faltasDoUsuario = await _context.Faltas
                                            .Where(f => f.UtilizadorId == userId)
                                            .OrderByDescending(f => f.DataFalta)
                                            .ThenBy(f => f.Inicio)
                                            .Select(f => new FaltaComUserNameViewModel
                                            {
                                                Id = f.Id,
                                                DataFalta = f.DataFalta,
                                                Inicio = f.Inicio,
                                                Fim = f.Fim,
                                                Motivo = f.Motivo,
                                                DataRegisto = f.Data,
                                                UserName = f.User != null ? (f.User.NomeCompleto ?? f.User.UserName ?? "N/A") : "N/A"
                                            })
                                            .ToListAsync(); // Agora deve funcionar
            return View(faltasDoUsuario);
        }

        // Exemplo de como um Admin/Office poderia ver todas as faltas:
        [Authorize(Roles = "Admin,Office", AuthenticationSchemes = "Identity.Application,AdminLoginScheme,CollaboratorLoginScheme")]
        public async Task<IActionResult> TodasAsFaltas(string? filtroNome, DateTime? filtroData)
        {
            IQueryable<Falta> query = _context.Faltas.Include(f => f.User); // Include agora deve funcionar

            if (!string.IsNullOrEmpty(filtroNome))
            {
                query = query.Where(f => f.User != null && f.User.UserName != null && f.User.UserName.Contains(filtroNome));
            }
            if (filtroData.HasValue)
            {
                query = query.Where(f => f.DataFalta.Date == filtroData.Value.Date);
            }

            var todasAsFaltas = await query
                                    .OrderByDescending(f => f.DataFalta)
                                    .ThenBy(f => f.User != null ? f.User.UserName : "")
                                    .ThenBy(f => f.Inicio)
                                    .Select(f => new FaltaComUserNameViewModel
                                    {
                                        Id = f.Id,
                                        UserName = f.User != null ? (f.User.NomeCompleto ?? f.User.UserName ?? "Desconhecido") : "Desconhecido",
                                        DataFalta = f.DataFalta,
                                        Inicio = f.Inicio,
                                        Fim = f.Fim,
                                        Motivo = f.Motivo,
                                        DataRegisto = f.Data
                                    })
                                    .ToListAsync(); // Agora deve funcionar
            return View(todasAsFaltas);
        }
    }
}