// Controllers/Api/FaltasApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SIGHR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminGeneralApiAccess")] // Segurança para toda a API
    public class FaltasApiController : ControllerBase
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<FaltasApiController> _logger;

        public FaltasApiController(SIGHRContext context, ILogger<FaltasApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// API para listar todas as faltas, com opções de filtro por nome e data.
        /// Rota: GET api/FaltasApi/ListarTodas
        /// </summary>
        [HttpGet("ListarTodas")]
        public async Task<IActionResult> ListarTodas(string? filtroNome, DateTime? filtroData)
        {
            try
            {
                _logger.LogInformation("API ListarTodas chamada com filtroNome: {FiltroNome}, filtroData: {FiltroData}", filtroNome, filtroData);
                IQueryable<Falta> query = _context.Faltas.Include(f => f.User);

                if (!string.IsNullOrEmpty(filtroNome)) query = query.Where(f => f.User != null && ((f.User.UserName != null && f.User.UserName.Contains(filtroNome)) || (f.User.NomeCompleto != null && f.User.NomeCompleto.Contains(filtroNome))));
                if (filtroData.HasValue) query = query.Where(f => f.DataFalta.Date == filtroData.Value.Date);

                var faltas = await query
                    .OrderByDescending(f => f.DataFalta)
                    .ThenBy(f => f.User != null ? f.User.UserName : "")
                    .ThenBy(f => f.Inicio)
                    .Select(f => new {
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
                return StatusCode(500, new { message = "Erro ao processar o pedido de listagem de faltas." });
            }
        }

        /// <summary>
        /// API para excluir uma ou mais faltas em massa.
        /// Rota: POST api/FaltasApi/Excluir
        /// </summary>
        [HttpPost("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromBody] List<long> idsParaExcluir)
        {
            if (idsParaExcluir == null || !idsParaExcluir.Any()) return BadRequest(new { message = "Nenhum ID de falta fornecido." });

            _logger.LogInformation("API Excluir chamada para os IDs: {Ids} pelo utilizador {User}", string.Join(", ", idsParaExcluir), User.Identity?.Name);
            try
            {
                var faltasParaRemover = await _context.Faltas.Where(f => idsParaExcluir.Contains(f.Id)).ToListAsync();
                if (!faltasParaRemover.Any()) return NotFound(new { message = "Nenhuma das faltas selecionadas foi encontrada." });

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