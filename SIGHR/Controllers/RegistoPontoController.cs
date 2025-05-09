namespace SIGHR.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using SIGHR.Data;
    using SIGHR.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Claims;

    [Authorize] // Apenas utilizadores autenticados podem acessar
    public class RegistoPontoController : Controller
    {
        private readonly SIGHRContext _context;

        public RegistoPontoController(SIGHRContext context)
        {
            _context = context;
        }

        // Método para registar a entrada
        [HttpPost]
        public async Task<IActionResult> RegistarEntrada()
        {
            var utilizadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);  // Obtém o ID do utilizador autenticado

            var registoExistente = _context.Horarios
                .FirstOrDefault(r => r.UtilizadorId == long.Parse(utilizadorId) && r.Data.Date == DateTime.Today.Date);

            if (registoExistente != null)
            {
                return BadRequest("Você já registrou a entrada para hoje.");
            }

            var registo = new Horario
            {
                UtilizadorId = long.Parse(utilizadorId),
                Data = DateTime.Today,
                HoraEntrada = DateTime.Now,
                HoraSaida = DateTime.MinValue, // Será atualizado na saída
                EntradaAlmoco = DateTime.MinValue,
                SaidaAlmoco = DateTime.MinValue
            };

            _context.Horarios.Add(registo);
            await _context.SaveChangesAsync();

            return Ok("Entrada registrada com sucesso!");
        }

        // Método para registar a saída
        [HttpPost]
        public async Task<IActionResult> RegistarSaida()
        {
            var utilizadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);  // Obtém o ID do utilizador autenticado

            var registo = _context.Horarios
                .FirstOrDefault(r => r.UtilizadorId == long.Parse(utilizadorId) && r.Data.Date == DateTime.Today.Date && r.HoraSaida == DateTime.MinValue);

            if (registo == null)
            {
                return BadRequest("Você não registrou a entrada para hoje.");
            }

            registo.HoraSaida = DateTime.Now;  // Atualiza a hora de saída

            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();

            return Ok("Saída registrada com sucesso!");
        }

        // Método para registar a saída para o almoço
        [HttpPost]
        public async Task<IActionResult> RegistarSaidaAlmoco()
        {
            var utilizadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var registo = _context.Horarios
                .FirstOrDefault(r => r.UtilizadorId == long.Parse(utilizadorId) && r.Data.Date == DateTime.Today.Date);

            if (registo == null)
            {
                return BadRequest("Você não registrou a entrada para hoje.");
            }

            registo.SaidaAlmoco = DateTime.Now;  // Registra a saída para o almoço

            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();

            return Ok("Saída para o almoço registrada com sucesso!");
        }

        // Método para registar a entrada do almoço
        [HttpPost]
        public async Task<IActionResult> RegistarEntradaAlmoco()
        {
            var utilizadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var registo = _context.Horarios
                .FirstOrDefault(r => r.UtilizadorId == long.Parse(utilizadorId) && r.Data.Date == DateTime.Today.Date);

            if (registo == null)
            {
                return BadRequest("Você não registrou a saída para o almoço.");
            }

            registo.EntradaAlmoco = DateTime.Now;  // Registra a entrada de volta do almoço

            _context.Horarios.Update(registo);
            await _context.SaveChangesAsync();

            return Ok("Entrada após o almoço registrada com sucesso!");
        }
    }
}
