using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaltasApiController : ControllerBase
    {
        private readonly SIGHRContext _context;

        public FaltasApiController(SIGHRContext context) => _context = context;

        /// <summary>
        /// Retorna todas as faltas registradas.
        /// </summary>
        /// <returns>Lista de objetos <see cref="Falta"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Falta>>> Get() => await _context.Faltas.ToListAsync();

        /// <summary>
        /// Retorna uma falta específica pelo ID.
        /// </summary>
        /// <param name="id">ID da falta.</param>
        /// <returns>Objeto <see cref="Falta"/> correspondente ao ID, ou NotFound se não existir.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Falta>> Get(long id)
        {
            var item = await _context.Faltas.FindAsync(id);
            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Atualiza os dados de uma falta existente.
        /// </summary>
        /// <param name="id">ID da falta a ser atualizada.</param>
        /// <param name="model">Dados atualizados da falta.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, Falta model)
        {
            if (id != model.Id) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Faltas.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Cria uma nova falta.
        /// </summary>
        /// <param name="model">Dados da nova falta.</param>
        /// <returns>Falta criada com status 201 Created.</returns>
        [HttpPost]
        public async Task<ActionResult<Falta>> Post(Falta model)
        {
            _context.Faltas.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        /// <summary>
        /// Remove uma falta existente.
        /// </summary>
        /// <param name="id">ID da falta a ser removida.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var item = await _context.Faltas.FindAsync(id);
            if (item == null) return NotFound();
            _context.Faltas.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
