// Controllers/Api/EncomendasApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;
using SIGHR.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace SIGHR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminGeneralApiAccess")] // Segurança para toda a API
    public class EncomendasApiController : ControllerBase
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<EncomendasApiController> _logger;

        public EncomendasApiController(SIGHRContext context, ILogger<EncomendasApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para obter a lista de todas as encomendas, com suporte para filtros.
        /// Rota: GET api/EncomendasApi/ListarComFiltros
        /// </summary>
        [HttpGet("ListarComFiltros")]
        public async Task<IActionResult> ListarComFiltros(string? filtroClienteObra, DateTime? filtroData, string? filtroEstado)
        {
            _logger.LogInformation("API ListarComFiltros. Cliente/Obra: {FCO}, Data: {FD}, Estado: {FE}", filtroClienteObra, filtroData, filtroEstado);
            try
            {
                IQueryable<Encomenda> query = _context.Encomendas
                    .Include(e => e.User)
                    .Include(e => e.Requisicoes!).ThenInclude(r => r.Material);

                if (!string.IsNullOrEmpty(filtroClienteObra)) query = query.Where(e => e.User != null && ((e.User.NomeCompleto != null && e.User.NomeCompleto.Contains(filtroClienteObra)) || (e.User.UserName != null && e.User.UserName.Contains(filtroClienteObra))) || (e.DescricaoObra != null && e.DescricaoObra.Contains(filtroClienteObra)));
                if (filtroData.HasValue) query = query.Where(e => e.Data.Date == filtroData.Value.Date);
                if (!string.IsNullOrEmpty(filtroEstado)) query = query.Where(e => e.Estado == filtroEstado);

                var encomendas = await query.OrderByDescending(e => e.Data)
                    .Select(e => new {
                        encomendaId = e.Id,
                        nomeCliente = e.User != null ? (e.User.NomeCompleto ?? e.User.UserName ?? "N/D") : "N/D",
                        dataEncomenda = e.Data.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        nomeObra = e.DescricaoObra ?? "N/D",
                        descricao = (e.Requisicoes != null && e.Requisicoes.Any()) ? string.Join(", ", e.Requisicoes.Where(r => r.Material != null).Select(r => r.Material!.Descricao ?? "").Take(3)) + (e.Requisicoes.Count > 3 ? "..." : "") : "Sem itens",
                        estado = e.Estado ?? "Indefinido"
                    }).ToListAsync();
                return Ok(encomendas);
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro na API ListarEncomendas"); return StatusCode(500, "Erro interno do servidor."); }
        }

        /// <summary>
        /// Endpoint para excluir uma ou mais encomendas em massa.
        /// Rota: POST api/EncomendasApi/Excluir
        /// </summary>
        [HttpPost("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromBody] List<long> idsParaExcluir)
        {
            if (idsParaExcluir == null || !idsParaExcluir.Any()) return BadRequest(new { message = "Nenhum ID fornecido." });
            try
            {
                var requisicoes = await _context.Requisicoes.Where(r => idsParaExcluir.Contains(r.EncomendaId)).ToListAsync();
                if (requisicoes.Any()) _context.Requisicoes.RemoveRange(requisicoes);
                var encomendas = await _context.Encomendas.Where(e => idsParaExcluir.Contains(e.Id)).ToListAsync();
                if (!encomendas.Any()) return NotFound(new { message = "Nenhuma encomenda encontrada para os IDs fornecidos." });
                _context.Encomendas.RemoveRange(encomendas);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{encomendas.Count} encomenda(s) excluída(s) com sucesso." });
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro na API ExcluirEncomendas"); return StatusCode(500, "Erro interno ao excluir as encomendas."); }
        }

        /// <summary>
        /// Endpoint para alterar o estado de uma encomenda específica.
        /// Rota: POST api/EncomendasApi/MudarEstado
        /// </summary>
        [HttpPost("MudarEstado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MudarEstado([FromBody] MudarEstadoEncomendaRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _logger.LogInformation("API MudarEstadoEncomenda: ID {Id}, Novo Estado {NovoEstado}, por {User}", request.Id, request.NovoEstado, User.Identity?.Name);
            try
            {
                var encomenda = await _context.Encomendas.FindAsync(request.Id);
                if (encomenda == null) return NotFound(new { message = $"Encomenda com ID {request.Id} não encontrada." });
                var estadosValidos = new List<string> { "Pendente", "Em Processamento", "Pronta para Envio", "Enviada", "Entregue", "Cancelada" };
                if (!estadosValidos.Contains(request.NovoEstado)) return BadRequest(new { message = $"O estado '{request.NovoEstado}' é inválido." });
                encomenda.Estado = request.NovoEstado;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Estado da encomenda {request.Id} atualizado para '{request.NovoEstado}'." });
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro na API MudarEstadoEncomenda para ID: {Id}", request.Id); return StatusCode(500, "Erro interno ao mudar o estado."); }
        }
    }
}