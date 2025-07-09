using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MateriaisApiController : ControllerBase
    {
        private readonly SIGHRContext _context;
        public MateriaisApiController(SIGHRContext context) => _context = context;

        /// <summary>
        /// Retorna todos os materiais cadastrados.
        /// </summary>
        /// <returns>Lista de objetos <see cref="Material"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> Get() => await _context.Materiais.ToListAsync();

        /// <summary>
        /// Retorna um material específico pelo ID.
        /// </summary>
        /// <param name="id">ID do material.</param>
        /// <returns>Objeto <see cref="Material"/> correspondente ao ID, ou NotFound se não for encontrado.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> Get(long id)
        {
            var item = await _context.Materiais.FindAsync(id);
            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Atualiza os dados de um material existente.
        /// </summary>
        /// <param name="id">ID do material a ser atualizado.</param>
        /// <param name="model">Dados atualizados do material.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, Material model)
        {
            if (id != model.Id) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Materiais.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Cria um novo material.
        /// </summary>
        /// <param name="model">Dados do novo material.</param>
        /// <returns>Material criado com status 201 Created.</returns>
        [HttpPost]
        public async Task<ActionResult<Material>> Post(Material model)
        {
            _context.Materiais.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        /// <summary>
        /// Remove um material existente.
        /// </summary>
        /// <param name="id">ID do material a ser removido.</param>
        /// <returns>Status HTTP indicando o resultado da operação.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var item = await _context.Materiais.FindAsync(id);
            if (item == null) return NotFound();
            _context.Materiais.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
