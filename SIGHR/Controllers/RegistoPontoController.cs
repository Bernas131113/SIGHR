namespace SIGHR.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc; // Certifique-se que este é o namespace correto para SIGHRContext e SIGHRUser
    using SIGHR.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Claims;
    using Microsoft.EntityFrameworkCore; // Para FirstOrDefaultAsync
    using SIGHR.Areas.Identity.Data;

    [Authorize] // Apenas utilizadores autenticados podem acessar
    public class RegistoPontoController : Controller
    {
        private readonly SIGHRContext _context;

        public RegistoPontoController(SIGHRContext context)
        {
            _context = context;
        }

        private string? GetCurrentUserId() // Adiciona '?' ao tipo de retorno
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Método para registar a entrada
        [HttpPost]
        public async Task<IActionResult> RegistarEntrada()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                return Unauthorized("Utilizador não autenticado."); // Ou BadRequest
            }

            var hoje = DateTime.Today; // Para consistência na data

            // Verifica se já existe um registo para o utilizador e data de hoje
            var registoExistente = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registoExistente != null)
            {
                // Poderia verificar se a HoraEntrada é TimeSpan.Zero para permitir um re-registo se algo deu errado
                // Mas por agora, uma entrada por dia é a regra.
                return BadRequest("Você já registrou a entrada para hoje.");
            }

            var registo = new Horario
            {
                UtilizadorId = utilizadorId, // Agora é string
                Data = hoje,
                HoraEntrada = DateTime.Now.TimeOfDay, // Converte para TimeSpan
                // Inicializar Saidas e Almoço com TimeSpan.Zero ou deixar como default (que é Zero)
                // TimeSpan.Zero indica que ainda não foi registrado
                HoraSaida = TimeSpan.Zero,
                EntradaAlmoco = TimeSpan.Zero,
                SaidaAlmoco = TimeSpan.Zero
            };

            _context.Horarios.Add(registo);
            await _context.SaveChangesAsync();

            return Ok("Entrada registrada com sucesso!");
        }

        // Método para registar a saída
        [HttpPost]
        public async Task<IActionResult> RegistarSaida()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            var hoje = DateTime.Today;

            // Procura um registo de entrada aberto para hoje
            var registo = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId &&
                                       r.Data.Date == hoje &&
                                       r.HoraSaida == TimeSpan.Zero); // Verifica se a saída ainda não foi registrada

            if (registo == null)
            {
                return BadRequest("Não foi encontrado um registo de entrada aberto para hoje ou a saída já foi registrada.");
            }

            // Verifica se já saiu para almoço e ainda não voltou
            if (registo.SaidaAlmoco != TimeSpan.Zero && registo.EntradaAlmoco == TimeSpan.Zero)
            {
                return BadRequest("Você precisa registrar a entrada do almoço antes de registrar a saída do dia.");
            }

            registo.HoraSaida = DateTime.Now.TimeOfDay; // Atualiza a hora de saída

            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();

            return Ok("Saída registrada com sucesso!");
        }

        // Método para registar a saída para o almoço
        [HttpPost]
        public async Task<IActionResult> RegistarSaidaAlmoco()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                return Unauthorized("Utilizador não autenticado.");
            }

            var hoje = DateTime.Today;

            var registo = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registo == null)
            {
                return BadRequest("Você não registrou a entrada para hoje.");
            }
            if (registo.HoraEntrada == TimeSpan.Zero) // Se a entrada não foi feita
            {
                return BadRequest("Você precisa registrar a entrada do dia primeiro.");
            }
            if (registo.SaidaAlmoco != TimeSpan.Zero)
            {
                return BadRequest("Você já registrou a saída para o almoço hoje.");
            }
            if (registo.HoraSaida != TimeSpan.Zero)
            {
                return BadRequest("Você já registrou a saída do dia. Não é possível registrar saída para almoço.");
            }


            registo.SaidaAlmoco = DateTime.Now.TimeOfDay;

            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();

            return Ok("Saída para o almoço registrada com sucesso!");
        }

        // Método para registar a entrada do almoço
        [HttpPost]
        public async Task<IActionResult> RegistarEntradaAlmoco()
        {
            var utilizadorId = GetCurrentUserId();
            if (string.IsNullOrEmpty(utilizadorId))
            {
                return Unauthorized("Utilizador não autenticado.");
            }
            var hoje = DateTime.Today;

            var registo = await _context.Horarios
                .FirstOrDefaultAsync(r => r.UtilizadorId == utilizadorId && r.Data.Date == hoje);

            if (registo == null)
            {
                // Este caso não deveria acontecer se a lógica de SaidaAlmoco for seguida
                return BadRequest("Registro de ponto diário não encontrado.");
            }
            if (registo.SaidaAlmoco == TimeSpan.Zero)
            {
                return BadRequest("Você não registrou a saída para o almoço ainda.");
            }
            if (registo.EntradaAlmoco != TimeSpan.Zero)
            {
                return BadRequest("Você já registrou a entrada de volta do almoço hoje.");
            }
            if (registo.HoraSaida != TimeSpan.Zero)
            {
                return BadRequest("Você já registrou a saída do dia. Não é possível registrar entrada de almoço.");
            }

            registo.EntradaAlmoco = DateTime.Now.TimeOfDay;

            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();

            return Ok("Entrada após o almoço registrada com sucesso!");
        }
    }
}