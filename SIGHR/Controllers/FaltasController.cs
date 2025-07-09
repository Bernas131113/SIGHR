// Controllers/FaltasController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Models;
using SIGHR.Models.ViewModels;
using System;
using System.Collections.Generic; // Para List<T>
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Controllers
{
    // A autorização no nível do controller pode ser mais genérica se as actions tiverem a sua própria.
    // Ou, se a maioria das actions for para colaboradores/office/admin, pode ser:
    [Authorize]
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

        // --- ACTIONS PARA COLABORADORES (E ADMINS/OFFICE PARA SUAS PRÓPRIAS FALTAS) ---

        // GET: /Faltas/Registar (Qualquer usuário logado e autorizado nos roles acima pode registrar UMA FALTA PARA SI MESMO)
        [HttpGet]
        [Authorize(Policy = "CollaboratorAccessUI")]
        public IActionResult Registar()
        {
            var model = new FaltaViewModel
            {
                DataFalta = DateTime.Today,
                Motivo = string.Empty // Se Motivo no ViewModel for 'required string' sem inicializador padrão
            };
            return View(model); // Retorna Views/Faltas/Registar.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CollaboratorAccessUI")]
        public async Task<IActionResult> Registar(FaltaViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            if (model.Inicio >= model.Fim)
            {
                ModelState.AddModelError(nameof(model.Fim), "A hora de fim deve ser posterior à hora de início.");
            }

            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.FindByIdAsync(userId); // Para logging
                string userNameForLog = currentUser?.UserName ?? "UsuárioDesconhecido";

                var falta = new Falta
                {
                    UtilizadorId = userId, // A falta é SEMPRE para o usuário logado aqui
                    Data = DateTime.Now,
                    DataFalta = model.DataFalta,
                    Inicio = model.Inicio,
                    Fim = model.Fim,
                    Motivo = model.Motivo
                };

                _context.Faltas.Add(falta);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Falta registrada com sucesso pelo usuário '{userNameForLog}' (ID: {userId}) para si mesmo.");
                TempData["SuccessMessage"] = "Falta registrada com sucesso!";
                return RedirectToAction(nameof(MinhasFaltas));
            }

            return View(model);
        }

        // GET: /Faltas/MinhasFaltas (Qualquer usuário logado e autorizado pode ver AS SUAS PRÓPRIAS FALTAS)
        [HttpGet]
        [Authorize(Policy = "CollaboratorAccessUI")]
        public async Task<IActionResult> MinhasFaltas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            var faltasDoUsuario = await _context.Faltas
                .Where(f => f.UtilizadorId == userId) // Filtra apenas as faltas do usuário logado
                .Include(f => f.User) // Opcional: se quiser mostrar o nome do usuário (será sempre o mesmo aqui)
                .OrderByDescending(f => f.DataFalta).ThenBy(f => f.Inicio)
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
                .ToListAsync();
            return View(faltasDoUsuario); // Retorna Views/Faltas/MinhasFaltas.cshtml
        }


        // --- ACTIONS PARA ADMINISTRAÇÃO (ADMIN/OFFICE) ---

        // GET: /Faltas/GestaoAdmin (Nome da action para a view de gestão de todas as faltas)
        [HttpGet]
        [Authorize(Policy = "AdminAccessUI")]
        public IActionResult GestaoAdmin()
        {
            ViewData["Title"] = "Gestão de Todas as Faltas";
            return View(); // Retorna Views/Faltas/GestaoAdmin.cshtml
        }

        // GET API: /Faltas/ListarTodasApi (API para popular a tabela de gestão do admin)
        [HttpGet]
        [Authorize(Policy = "AdminGeneralApiAccess")]
        public async Task<IActionResult> ListarTodasApi(string? filtroNome, DateTime? filtroData)
        {
            try
            {
                _logger.LogInformation("API ListarTodasApi chamada com filtroNome: {FiltroNome}, filtroData: {FiltroData}", filtroNome, filtroData);
                IQueryable<Falta> query = _context.Faltas.Include(f => f.User);

                if (!string.IsNullOrEmpty(filtroNome))
                {
                    query = query.Where(f => f.User != null &&
                                             ((f.User.UserName != null && f.User.UserName.Contains(filtroNome)) ||
                                              (f.User.NomeCompleto != null && f.User.NomeCompleto.Contains(filtroNome))));
                }
                if (filtroData.HasValue)
                {
                    query = query.Where(f => f.DataFalta.Date == filtroData.Value.Date);
                }

                var faltas = await query
                    .OrderByDescending(f => f.DataFalta)
                    .ThenBy(f => f.User != null ? f.User.UserName : "")
                    .ThenBy(f => f.Inicio)
                    .Select(f => new
                    {
                        faltaId = f.Id,
                        nomeUtilizador = f.User != null ? (f.User.NomeCompleto ?? f.User.UserName ?? "Desconhecido") : "Desconhecido",
                        dataFalta = f.DataFalta.ToString("yyyy-MM-dd"),
                        inicio = f.Inicio.ToString(@"hh\:mm\:ss"),
                        fim = f.Fim.ToString(@"hh\:mm\:ss"),
                        motivo = f.Motivo,
                        dataRegisto = f.Data.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();
                return Ok(faltas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar todas as faltas via API.");
                return StatusCode(500, new { message = "Erro ao processar a solicitação de listagem de faltas." });
            }
        }

        // POST API: /Faltas/ExcluirApi (API para excluir faltas selecionadas pelo admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminGeneralApiAccess")]
        public async Task<IActionResult> ExcluirApi([FromBody] List<long> idsParaExcluir)
        {
            if (idsParaExcluir == null || !idsParaExcluir.Any())
            {
                return BadRequest(new { message = "Nenhum ID de falta fornecido." });
            }
            _logger.LogInformation("API ExcluirApi chamada para IDs: {Ids} pelo usuário {User}", string.Join(", ", idsParaExcluir), User.Identity?.Name);
            try
            {
                var faltasParaRemover = await _context.Faltas.Where(f => idsParaExcluir.Contains(f.Id)).ToListAsync();
                if (!faltasParaRemover.Any())
                {
                    return NotFound(new { message = "Nenhuma das faltas selecionadas foi encontrada." });
                }
                _context.Faltas.RemoveRange(faltasParaRemover);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{faltasParaRemover.Count} falta(s) excluída(s) com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir faltas via API.");
                return StatusCode(500, new { message = "Ocorreu um erro ao excluir as faltas." });
            }
        }
    }
}