using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    /// <summary>
    /// Controlador de API para gerir as operações CRUD (Criar, Ler, Atualizar, Apagar) da entidade Requisicao.
    /// A entidade Requisicao funciona como uma tabela de junção entre Material e Encomenda.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RequisicoesApiController : ControllerBase
    {
        // O contexto da base de dados, injetado no construtor.
        private readonly SIGHRContext _context;

        public RequisicoesApiController(SIGHRContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Endpoint para obter a lista de todas as requisições.
        /// Rota: GET api/RequisicoesApi
        /// </summary>
        /// <returns>Uma lista de objetos do tipo <see cref="Requisicao"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Requisicao>>> Get()
        {
            return await _context.Requisicoes.ToListAsync();
        }

        /// <summary>
        /// Endpoint para obter uma requisição específica através da sua chave composta (MaterialId e EncomendaId).
        /// Rota: GET api/RequisicoesApi/{materialId}/{encomendaId}
        /// </summary>
        /// <param name="materialId">O ID do material associado à requisição.</param>
        /// <param name="encomendaId">O ID da encomenda associada à requisição.</param>
        /// <returns>O objeto <see cref="Requisicao"/> correspondente, ou 'NotFound' (404) se não existir.</returns>
        [HttpGet("{materialId}/{encomendaId}")]
        public async Task<ActionResult<Requisicao>> Get(long materialId, long encomendaId)
        {
            var item = await _context.Requisicoes
                .FirstOrDefaultAsync(r => r.MaterialId == materialId && r.EncomendaId == encomendaId);

            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Endpoint para atualizar uma requisição existente.
        /// Rota: PUT api/RequisicoesApi/{materialId}/{encomendaId}
        /// </summary>
        /// <param name="materialId">O ID do material da requisição a ser atualizada.</param>
        /// <param name="encomendaId">O ID da encomenda da requisição a ser atualizada.</param>
        /// <param name="model">O objeto <see cref="Requisicao"/> com os dados atualizados.</param>
        /// <returns>Um código de estado HTTP a indicar o resultado da operação.</returns>
        [HttpPut("{materialId}/{encomendaId}")]
        public async Task<IActionResult> Put(long materialId, long encomendaId, Requisicao model)
        {
            if (materialId != model.MaterialId || encomendaId != model.EncomendaId)
            {
                return BadRequest("Os IDs na URL não correspondem aos IDs no corpo do pedido.");
            }

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Requisicoes.Any(r => r.MaterialId == materialId && r.EncomendaId == encomendaId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent(); // Retorna 204 No Content, indicando sucesso.
        }

        /// <summary>
        /// Endpoint para criar uma nova requisição.
        /// Rota: POST api/RequisicoesApi
        /// </summary>
        /// <param name="model">O objeto <see cref="Requisicao"/> com os dados a serem criados.</param>
        /// <returns>A requisição recém-criada, com a sua chave e um código de estado 201 Created.</returns>
        [HttpPost]
        public async Task<ActionResult<Requisicao>> Post(Requisicao model)
        {
            _context.Requisicoes.Add(model);
            await _context.SaveChangesAsync();

            // Retorna a requisição criada e a URL para a aceder (boa prática REST).
            return CreatedAtAction(nameof(Get), new { materialId = model.MaterialId, encomendaId = model.EncomendaId }, model);
        }

        /// <summary>
        /// Endpoint para remover uma requisição existente.
        /// Rota: DELETE api/RequisicoesApi/{materialId}/{encomendaId}
        /// </summary>
        /// <param name="materialId">O ID do material da requisição a ser removida.</param>
        /// <param name="encomendaId">O ID da encomenda da requisição a ser removida.</param>
        /// <returns>Um código de estado HTTP a indicar o resultado da operação.</returns>
        [HttpDelete("{materialId}/{encomendaId}")]
        public async Task<IActionResult> Delete(long materialId, long encomendaId)
        {
            var item = await _context.Requisicoes
                .FirstOrDefaultAsync(r => r.MaterialId == materialId && r.EncomendaId == encomendaId);

            if (item == null)
            {
                return NotFound();
            }

            _context.Requisicoes.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204 No Content, indicando sucesso.
        }
    }
}