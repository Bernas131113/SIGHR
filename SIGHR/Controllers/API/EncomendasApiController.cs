using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class EncomendasApiController : ControllerBase
    {
        private readonly SIGHRContext _context;
        public EncomendasApiController(SIGHRContext context) => _context = context;

        /// <summary>
        /// Obtém a lista de todas as encomendas.
        /// </summary>
        /// <returns>Lista de objetos Encomenda.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Encomenda>>> Get() => await _context.Encomendas.ToListAsync();

        /// <summary>
        /// Obtém uma encomenda específica pelo ID.
        /// </summary>
        /// <param name="id">ID da encomenda.</param>
        /// <returns>Objeto Encomenda correspondente ao ID.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Encomenda>> Get(long id)
        {
            var item = await _context.Encomendas.FindAsync(id);
            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Atualiza uma encomenda existente.
        /// </summary>
        /// <param name="id">ID da encomenda a ser atualizada.</param>
        /// <param name="model">Objeto Encomenda com os dados atualizados.</param>
        /// <returns>Status da operação.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, Encomenda model)
        {
            if (id != model.Id) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Encomendas.Any(e => e.Id == id)) return NotFound(); else throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Cria uma nova encomenda.
        /// </summary>
        /// <param name="model">Objeto Encomenda a ser criado.</param>
        /// <returns>Encomenda criada com seu ID.</returns>
        [HttpPost]
        public async Task<ActionResult<Encomenda>> Post(Encomenda model)
        {
            _context.Encomendas.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        /// <summary>
        /// Remove uma encomenda existente.
        /// </summary>
        /// <param name="id">ID da encomenda a ser removida.</param>
        /// <returns>Status da operação.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var item = await _context.Encomendas.FindAsync(id);
            if (item == null) return NotFound();
            _context.Encomendas.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
