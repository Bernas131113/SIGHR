// Controllers/RegistoPontoController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;       // Para UserManager e IdentityConstants
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;    // Para Include, ToListAsync, etc.
using SIGHR.Areas.Identity.Data;                // Para SIGHRContext e SIGHRUser
using SIGHR.Models;                 // Para a entidade Horario
using SIGHR.Models.ViewModels;      // Para HorarioColaboradorViewModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;       // Para ClaimsPrincipal, FindFirstValue, ClaimTypes
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;   // Para ILogger

namespace SIGHR.Controllers
{
    // A autorização no nível do controller agora permite todos os roles que podem interagir com o ponto.
    // As actions individuais podem ter autorizações mais específicas se necessário.
    [Authorize(Roles = "Admin,Office,Collaborator", AuthenticationSchemes = "Identity.Application,AdminLoginScheme,CollaboratorLoginScheme")]
    public class RegistoPontoController : Controller
    {
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager; // Injetado para obter dados do usuário se necessário
        private readonly ILogger<RegistoPontoController> _logger;

        public RegistoPontoController(SIGHRContext context, UserManager<SIGHRUser> userManager, ILogger<RegistoPontoController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Método auxiliar para obter o ID do usuário logado
        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // --- ACTIONS PARA A INTERFACE DE BATER PONTO DO COLABORADOR ---
        // Se você tiver uma view para os botões de bater ponto, ela pode ser servida por uma action Index aqui.
        // Exemplo:
        // GET: /RegistoPonto/ ou /RegistoPonto/Index
        // [HttpGet]
        // public IActionResult Index()
        // {
        //     ViewData["Title"] = "Registo de Ponto";
        //     return View(); // Retornaria Views/RegistoPonto/Index.cshtml (onde estariam os botões)
        // }


        // GET: /RegistoPonto/GetPontoDoDia (para AJAX)
        [HttpGet]
        public async Task<IActionResult> GetPontoDoDia()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                return Unauthorized(new { message = "Utilizador não autenticado." });
            }

            var hoje = DateTime.Today;
            var registoDoDia = await _context.Horarios
                .Where(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje)
                .Select(r => new
                {
                    // Retorna TimeSpan.ToString() que já é "hh:mm:ss" ou similar
                    HoraEntrada = r.HoraEntrada.ToString(@"hh\:mm\:ss"),
                    SaidaAlmoco = r.SaidaAlmoco.ToString(@"hh\:mm\:ss"),
                    EntradaAlmoco = r.EntradaAlmoco.ToString(@"hh\:mm\:ss"),
                    HoraSaida = r.HoraSaida.ToString(@"hh\:mm\:ss")
                })
                .FirstOrDefaultAsync();

            if (registoDoDia == null)
            {
                // Se não há registo, retorna TimeSpans zerados formatados
                return Ok(new
                {
                    HoraEntrada = TimeSpan.Zero.ToString(@"hh\:mm\:ss"),
                    SaidaAlmoco = TimeSpan.Zero.ToString(@"hh\:mm\:ss"),
                    EntradaAlmoco = TimeSpan.Zero.ToString(@"hh\:mm\:ss"),
                    HoraSaida = TimeSpan.Zero.ToString(@"hh\:mm\:ss")
                });
            }
            return Ok(registoDoDia);
        }


        // POST: /RegistoPonto/RegistarEntrada
        [HttpPost]
        [ValidateAntiForgeryToken] // Importante para segurança
        public async Task<IActionResult> RegistarEntrada()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                _logger.LogWarning("RegistarEntrada: Tentativa de acesso não autenticado.");
                return Unauthorized(new { success = false, message = "Utilizador não autenticado." });
            }

            var hoje = DateTime.Today;
            var registoExistente = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registoExistente != null)
            {
                if (registoExistente.HoraEntrada == TimeSpan.Zero)
                {
                    registoExistente.HoraEntrada = DateTime.Now.TimeOfDay;
                    _context.Horarios.Update(registoExistente);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("RegistarEntrada: Entrada atualizada para o utilizador {UserId} às {HoraEntrada}.", utilizadorId, registoExistente.HoraEntrada);
                    return Ok(new { success = true, message = "Entrada registrada com sucesso!", hora = registoExistente.HoraEntrada.ToString(@"hh\:mm\:ss") });
                }
                _logger.LogWarning("RegistarEntrada: Utilizador {UserId} já registrou a entrada hoje.", utilizadorId);
                return BadRequest(new { success = false, message = "Você já registrou a entrada para hoje." });
            }

            var novoRegisto = new Horario
            {
                UtilizadorId = utilizadorId,
                Data = hoje,
                HoraEntrada = DateTime.Now.TimeOfDay,
                HoraSaida = TimeSpan.Zero, // Padrão para TimeSpan
                EntradaAlmoco = TimeSpan.Zero,
                SaidaAlmoco = TimeSpan.Zero
            };

            _context.Horarios.Add(novoRegisto);
            await _context.SaveChangesAsync();
            _logger.LogInformation("RegistarEntrada: Nova entrada registrada para o utilizador {UserId} às {HoraEntrada}.", utilizadorId, novoRegisto.HoraEntrada);
            return Ok(new { success = true, message = "Entrada registrada com sucesso!", hora = novoRegisto.HoraEntrada.ToString(@"hh\:mm\:ss") });
        }

        // POST: /RegistoPonto/RegistarSaidaAlmoco
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistarSaidaAlmoco()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId)) return Unauthorized(new { success = false, message = "Utilizador não autenticado." });

            var hoje = DateTime.Today;
            var registo = await _context.Horarios.FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registo == null || registo.HoraEntrada == TimeSpan.Zero) return BadRequest(new { success = false, message = "Registe a entrada do dia primeiro." });
            if (registo.SaidaAlmoco != TimeSpan.Zero) return BadRequest(new { success = false, message = "Já registrou a saída para almoço hoje." });
            if (registo.HoraSaida != TimeSpan.Zero) return BadRequest(new { success = false, message = "Já registrou a saída do dia." });

            registo.SaidaAlmoco = DateTime.Now.TimeOfDay;
            await _context.SaveChangesAsync();
            _logger.LogInformation("RegistarSaidaAlmoco: Saída para almoço para {UserId} às {SaidaAlmoco}.", utilizadorId, registo.SaidaAlmoco);
            return Ok(new { success = true, message = "Saída para almoço registrada!", hora = registo.SaidaAlmoco.ToString(@"hh\:mm\:ss") });
        }

        // POST: /RegistoPonto/RegistarEntradaAlmoco
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistarEntradaAlmoco()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId)) return Unauthorized(new { success = false, message = "Utilizador não autenticado." });

            var hoje = DateTime.Today;
            var registo = await _context.Horarios.FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registo == null || registo.HoraEntrada == TimeSpan.Zero) return BadRequest(new { success = false, message = "Registo de entrada não encontrado." });
            if (registo.SaidaAlmoco == TimeSpan.Zero) return BadRequest(new { success = false, message = "Registe a saída para almoço primeiro." });
            if (registo.EntradaAlmoco != TimeSpan.Zero) return BadRequest(new { success = false, message = "Já registou a entrada do almoço." });
            if (registo.HoraSaida != TimeSpan.Zero) return BadRequest(new { success = false, message = "Já registou a saída do dia." });

            registo.EntradaAlmoco = DateTime.Now.TimeOfDay;
            await _context.SaveChangesAsync();
            _logger.LogInformation("RegistarEntradaAlmoco: Entrada do almoço para {UserId} às {EntradaAlmoco}.", utilizadorId, registo.EntradaAlmoco);
            return Ok(new { success = true, message = "Entrada do almoço registrada!", hora = registo.EntradaAlmoco.ToString(@"hh\:mm\:ss") });
        }

        // POST: /RegistoPonto/RegistarSaida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistarSaida()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId)) return Unauthorized(new { success = false, message = "Utilizador não autenticado." });

            var hoje = DateTime.Today;
            var registo = await _context.Horarios.FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje && r.HoraEntrada != TimeSpan.Zero && r.HoraSaida == TimeSpan.Zero);

            if (registo == null) return BadRequest(new { success = false, message = "Registo de entrada não encontrado ou saída já efetuada." });
            if (registo.SaidaAlmoco != TimeSpan.Zero && registo.EntradaAlmoco == TimeSpan.Zero) return BadRequest(new { success = false, message = "Registe a entrada do almoço primeiro." });

            registo.HoraSaida = DateTime.Now.TimeOfDay;
            await _context.SaveChangesAsync();
            _logger.LogInformation("RegistarSaida: Saída do dia para {UserId} às {HoraSaida}.", utilizadorId, registo.HoraSaida);
            return Ok(new { success = true, message = "Saída registrada com sucesso!", hora = registo.HoraSaida.ToString(@"hh\:mm\:ss") });
        }


        // --- ACTION PARA A VIEW "MEU REGISTO DE PONTO" (COLABORADOR) ---
        [HttpGet]
        // A autorização geral do controller já cobre isso.
        // Se quisesse ser mais específico, poderia adicionar [Authorize(Roles="Collaborator")] aqui,
        // mas o filtro por userId já garante que ele só veja os seus.
        public async Task<IActionResult> MeuRegisto(DateTime? filtroData)
        {
            ViewData["Title"] = "Meu Registo de Ponto";
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Acesso a MeuRegisto sem UserID.");
                return Unauthorized("Utilizador não autenticado.");
            }

            IQueryable<Horario> query = _context.Horarios
                                            .Where(h => h.UtilizadorId == userId);

            if (filtroData.HasValue)
            {
                query = query.Where(h => h.Data.Date == filtroData.Value.Date);
            }

            var horariosDoUsuario = await query
                                        .OrderByDescending(h => h.Data)
                                        .ThenBy(h => h.HoraEntrada)
                                        .ToListAsync();

            var viewModels = horariosDoUsuario.Select(h =>
            {
                TimeSpan totalTrabalhado = TimeSpan.Zero;
                TimeSpan tempoAlmoco = TimeSpan.Zero;

                if (h.EntradaAlmoco != TimeSpan.Zero && h.SaidaAlmoco != TimeSpan.Zero && h.EntradaAlmoco > h.SaidaAlmoco)
                {
                    tempoAlmoco = h.EntradaAlmoco - h.SaidaAlmoco;
                }

                if (h.HoraSaida != TimeSpan.Zero && h.HoraEntrada != TimeSpan.Zero && h.HoraSaida > h.HoraEntrada)
                {
                    totalTrabalhado = (h.HoraSaida - h.HoraEntrada) - tempoAlmoco;
                    if (totalTrabalhado < TimeSpan.Zero) totalTrabalhado = TimeSpan.Zero;
                }

                return new HorarioColaboradorViewModel // Ou HorarioAdminViewModel se for o mesmo
                {
                    HorarioId = h.Id,
                    Data = h.Data,
                    HoraEntrada = h.HoraEntrada,
                    SaidaAlmoco = h.SaidaAlmoco,
                    EntradaAlmoco = h.EntradaAlmoco,
                    HoraSaida = h.HoraSaida,
                    TotalHorasTrabalhadas = totalTrabalhado > TimeSpan.Zero ? $"{(int)totalTrabalhado.TotalHours:D2}:{totalTrabalhado.Minutes:D2}" : "--:--",
                    // Localizacao = h.Localizacao; // Se tiver este campo
                };
            }).ToList();

            ViewData["FiltroDataAtual"] = filtroData?.ToString("yyyy-MM-dd");

            return View(viewModels); // Retorna Views/RegistoPonto/MeuRegisto.cshtml
        }
    }
}