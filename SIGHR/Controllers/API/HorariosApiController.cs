using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class HorariosApiController : ControllerBase
    {
        private readonly SIGHRContext _context;

        public HorariosApiController(SIGHRContext context) => _context = context;

        /// <summary>
        /// Retorna todos os horários cadastrados.
        /// </summary>
        /// <returns>Lista de objetos <see cref="Horario"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Horario>>> Get() => await _context.Horarios.ToListAsync();

        /// <summary>
        /// Retorna um horário específico pelo ID.
        /// </summary>
        /// <param name="id">ID do horário.</param>
        /// <returns>Objeto <see cref="Horario"/> correspondente ao ID, ou NotFound se não for encontrado.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Horario>> Get(long id)
        {
            var item = await _context.Horarios.FindAsync(id);
            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Atualiza os dados de um horário existente.
        /// </summary>
        /// <param name="id">ID do horário a ser atualizado.</param>
        /// <param name="model">Dados atualizados do horário.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, Horario model)
        {
            if (id != model.Id) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Horarios.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Cria um novo horário.
        /// </summary>
        /// <param name="model">Dados do novo horário.</param>
        /// <returns>Horário criado com status 201 Created.</returns>
        [HttpPost]
        public async Task<ActionResult<Horario>> Post(Horario model)
        {
            _context.Horarios.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        /// <summary>
        /// Remove um horário existente.
        /// </summary>
        /// <param name="id">ID do horário a ser removido.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var item = await _context.Horarios.FindAsync(id);
            if (item == null) return NotFound();
            _context.Horarios.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
