// Controllers/RegistoPontoController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models; // Namespace para a entidade Horario
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore; // Para FirstOrDefaultAsync
using Microsoft.Extensions.Logging;   // Para ILogger

namespace SIGHR.Controllers
{
    // Protege todo o controller, permitindo acesso apenas a usuários autenticados
    // com o CollaboratorLoginScheme. Adicione roles se necessário (ex: "Collaborator", "Office").
    [Authorize(AuthenticationSchemes = "CollaboratorLoginScheme")]
    public class RegistoPontoController : Controller
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<RegistoPontoController> _logger;

        public RegistoPontoController(SIGHRContext context, ILogger<RegistoPontoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Método auxiliar para obter o ID do usuário logado
        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

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
                    HoraEntrada = r.HoraEntrada.ToString(@"hh\:mm\:ss"),
                    SaidaAlmoco = r.SaidaAlmoco.ToString(@"hh\:mm\:ss"),
                    EntradaAlmoco = r.EntradaAlmoco.ToString(@"hh\:mm\:ss"),
                    HoraSaida = r.HoraSaida.ToString(@"hh\:mm\:ss")
                })
                .FirstOrDefaultAsync();

            if (registoDoDia == null)
            {
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
        [ValidateAntiForgeryToken] // Boa prática para POSTs que alteram dados
        public async Task<IActionResult> RegistarEntrada()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                _logger.LogWarning("RegistarEntrada: Tentativa de acesso não autenticado.");
                return Unauthorized("Utilizador não autenticado.");
            }

            var hoje = DateTime.Today;
            var registoExistente = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registoExistente != null)
            {
                // Se já existe, mas a entrada não foi marcada (HoraEntrada é Zero), permite marcar.
                // Isso pode acontecer se o registro foi criado por outra ação ou se houve um erro.
                if (registoExistente.HoraEntrada == TimeSpan.Zero)
                {
                    registoExistente.HoraEntrada = DateTime.Now.TimeOfDay;
                    _context.Horarios.Update(registoExistente);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"RegistarEntrada: Entrada atualizada para o utilizador {utilizadorId} às {registoExistente.HoraEntrada}.");
                    return Ok("Entrada registrada com sucesso!");
                }
                _logger.LogWarning($"RegistarEntrada: Utilizador {utilizadorId} já registrou a entrada hoje.");
                return BadRequest("Você já registrou a entrada para hoje.");
            }

            var novoRegisto = new Horario
            {
                UtilizadorId = utilizadorId,
                Data = hoje,
                HoraEntrada = DateTime.Now.TimeOfDay,
                HoraSaida = TimeSpan.Zero,
                EntradaAlmoco = TimeSpan.Zero,
                SaidaAlmoco = TimeSpan.Zero
            };

            _context.Horarios.Add(novoRegisto);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"RegistarEntrada: Nova entrada registrada para o utilizador {utilizadorId} às {novoRegisto.HoraEntrada}.");
            return Ok("Entrada registrada com sucesso!");
        }

        // POST: /RegistoPonto/RegistarSaidaAlmoco
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistarSaidaAlmoco()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                _logger.LogWarning("RegistarSaidaAlmoco: Tentativa de acesso não autenticado.");
                return Unauthorized("Utilizador não autenticado.");
            }

            var hoje = DateTime.Today;
            var registo = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registo == null || registo.HoraEntrada == TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarSaidaAlmoco: Utilizador {utilizadorId} tentou sair para almoço sem registro de entrada.");
                return BadRequest("Você precisa registrar a entrada do dia primeiro.");
            }
            if (registo.SaidaAlmoco != TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarSaidaAlmoco: Utilizador {utilizadorId} já registrou saída para almoço.");
                return BadRequest("Você já registrou a saída para o almoço hoje.");
            }
            if (registo.HoraSaida != TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarSaidaAlmoco: Utilizador {utilizadorId} tentou sair para almoço após registrar saída do dia.");
                return BadRequest("Você já registrou a saída do dia. Não é possível registrar saída para almoço.");
            }

            registo.SaidaAlmoco = DateTime.Now.TimeOfDay;
            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"RegistarSaidaAlmoco: Saída para almoço registrada para {utilizadorId} às {registo.SaidaAlmoco}.");
            return Ok("Saída para o almoço registrada com sucesso!");
        }

        // POST: /RegistoPonto/RegistarEntradaAlmoco
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistarEntradaAlmoco()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                _logger.LogWarning("RegistarEntradaAlmoco: Tentativa de acesso não autenticado.");
                return Unauthorized("Utilizador não autenticado.");
            }

            var hoje = DateTime.Today;
            var registo = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registo == null || registo.HoraEntrada == TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarEntradaAlmoco: Utilizador {utilizadorId} tentou voltar do almoço sem registro de entrada.");
                return BadRequest("Registro de ponto diário não encontrado ou entrada não registrada.");
            }
            if (registo.SaidaAlmoco == TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarEntradaAlmoco: Utilizador {utilizadorId} tentou voltar do almoço sem ter saído para almoço.");
                return BadRequest("Você não registrou a saída para o almoço ainda.");
            }
            if (registo.EntradaAlmoco != TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarEntradaAlmoco: Utilizador {utilizadorId} já registrou entrada do almoço.");
                return BadRequest("Você já registrou a entrada de volta do almoço hoje.");
            }
            if (registo.HoraSaida != TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarEntradaAlmoco: Utilizador {utilizadorId} tentou voltar do almoço após registrar saída do dia.");
                return BadRequest("Você já registrou a saída do dia. Não é possível registrar entrada de almoço.");
            }

            registo.EntradaAlmoco = DateTime.Now.TimeOfDay;
            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"RegistarEntradaAlmoco: Entrada do almoço registrada para {utilizadorId} às {registo.EntradaAlmoco}.");
            return Ok("Entrada após o almoço registrada com sucesso!");
        }


        // POST: /RegistoPonto/RegistarSaida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistarSaida()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                _logger.LogWarning("RegistarSaida: Tentativa de acesso não autenticado.");
                return Unauthorized("Utilizador não autenticado.");
            }

            var hoje = DateTime.Today;
            var registo = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId &&
                                       r.Data.Date == hoje &&
                                       r.HoraEntrada != TimeSpan.Zero && // Deve ter entrado
                                       r.HoraSaida == TimeSpan.Zero);   // Saída ainda não registrada

            if (registo == null)
            {
                _logger.LogWarning($"RegistarSaida: Utilizador {utilizadorId} não encontrou registro de entrada aberto.");
                return BadRequest("Não foi encontrado um registo de entrada aberto para hoje ou a saída já foi registrada.");
            }

            // Se saiu para almoço, deve ter voltado
            if (registo.SaidaAlmoco != TimeSpan.Zero && registo.EntradaAlmoco == TimeSpan.Zero)
            {
                _logger.LogWarning($"RegistarSaida: Utilizador {utilizadorId} tentou sair do dia sem ter voltado do almoço.");
                return BadRequest("Você precisa registrar a entrada do almoço antes de registrar a saída do dia.");
            }

            registo.HoraSaida = DateTime.Now.TimeOfDay;
            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"RegistarSaida: Saída do dia registrada para {utilizadorId} às {registo.HoraSaida}.");
            return Ok("Saída registrada com sucesso!");
        }
    }
}