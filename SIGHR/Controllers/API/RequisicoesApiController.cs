using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequisicoesApiController : ControllerBase
    {
        private readonly SIGHRContext _context;
        public RequisicoesApiController(SIGHRContext context) => _context = context;

        /// <summary>
        /// Retorna todas as requisições cadastradas.
        /// </summary>
        /// <returns>Lista de objetos <see cref="Requisicao"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Requisicao>>> Get() => await _context.Requisicoes.ToListAsync();

        /// <summary>
        /// Retorna uma requisição específica com base no ID do material e da encomenda.
        /// </summary>
        /// <param name="materialId">ID do material relacionado à requisição.</param>
        /// <param name="encomendaId">ID da encomenda relacionada à requisição.</param>
        /// <returns>Objeto <see cref="Requisicao"/> correspondente, ou NotFound se não for encontrado.</returns>
        [HttpGet("{materialId}/{encomendaId}")]
        public async Task<ActionResult<Requisicao>> Get(long materialId, long encomendaId)
        {
            var item = await _context.Requisicoes
                .FirstOrDefaultAsync(r => r.MaterialId == materialId && r.EncomendaId == encomendaId);
            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Atualiza os dados de uma requisição existente.
        /// </summary>
        /// <param name="materialId">ID do material da requisição.</param>
        /// <param name="encomendaId">ID da encomenda da requisição.</param>
        /// <param name="model">Objeto <see cref="Requisicao"/> com os dados atualizados.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpPut("{materialId}/{encomendaId}")]
        public async Task<IActionResult> Put(long materialId, long encomendaId, Requisicao model)
        {
            if (materialId != model.MaterialId || encomendaId != model.EncomendaId) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Requisicoes.Any(r => r.MaterialId == materialId && r.EncomendaId == encomendaId))
                    return NotFound();
                else throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Cria uma nova requisição.
        /// </summary>
        /// <param name="model">Objeto <see cref="Requisicao"/> com os dados da nova requisição.</param>
        /// <returns>Requisição criada com status 201 Created.</returns>
        [HttpPost]
        public async Task<ActionResult<Requisicao>> Post(Requisicao model)
        {
            _context.Requisicoes.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { materialId = model.MaterialId, encomendaId = model.EncomendaId }, model);
        }

        /// <summary>
        /// Remove uma requisição existente com base no ID do material e da encomenda.
        /// </summary>
        /// <param name="materialId">ID do material da requisição.</param>
        /// <param name="encomendaId">ID da encomenda da requisição.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpDelete("{materialId}/{encomendaId}")]
        public async Task<IActionResult> Delete(long materialId, long encomendaId)
        {
            var item = await _context.Requisicoes
                .FirstOrDefaultAsync(r => r.MaterialId == materialId && r.EncomendaId == encomendaId);
            if (item == null) return NotFound();
            _context.Requisicoes.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
