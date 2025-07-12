using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    /// <summary>
    /// Controlador de API para gerir as operações CRUD (Criar, Ler, Atualizar, Apagar) da entidade Material.
    /// Define os endpoints que podem ser chamados por clientes HTTP (como JavaScript).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MateriaisApiController : ControllerBase
    {
        // O contexto da base de dados, injetado no construtor.
        private readonly SIGHRContext _context;

        public MateriaisApiController(SIGHRContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Endpoint para obter a lista de todos os materiais.
        /// Rota: GET api/MateriaisApi
        /// </summary>
        /// <returns>Uma lista de objetos do tipo <see cref="Material"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> Get()
        {
            return await _context.Materiais.ToListAsync();
        }

        /// <summary>
        /// Endpoint para obter um material específico através do seu ID.
        /// Rota: GET api/MateriaisApi/{id}
        /// </summary>
        /// <param name="id">O ID do material a ser procurado.</param>
        /// <returns>O objeto <see cref="Material"/> correspondente ao ID, ou 'NotFound' (404) se não existir.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> Get(long id)
        {
            var item = await _context.Materiais.FindAsync(id);
            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Endpoint para atualizar um material existente.
        /// Rota: PUT api/MateriaisApi/{id}
        /// </summary>
        /// <param name="id">O ID do material a ser atualizado.</param>
        /// <param name="model">O objeto Material com os dados atualizados.</param>
        /// <returns>Um código de estado HTTP a indicar o resultado da operação.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, Material model)
        {
            if (id != model.Id)
            {
                return BadRequest("O ID na URL não corresponde ao ID no corpo do pedido.");
            }

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Verifica se o material ainda existe, para evitar erros de concorrência.
                if (!_context.Materiais.Any(e => e.Id == id))
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
        /// Endpoint para criar um novo material.
        /// Rota: POST api/MateriaisApi
        /// </summary>
        /// <param name="model">O objeto Material com os dados a serem criados.</param>
        /// <returns>O material recém-criado, com o seu novo ID e um código de estado 201 Created.</returns>
        [HttpPost]
        public async Task<ActionResult<Material>> Post(Material model)
        {
            _context.Materiais.Add(model);
            await _context.SaveChangesAsync();

            // Retorna o material criado e a URL para o aceder (boa prática REST).
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        /// <summary>
        /// Endpoint para remover um material existente.
        /// Rota: DELETE api/MateriaisApi/{id}
        /// </summary>
        /// <param name="id">O ID do material a ser removido.</param>
        /// <returns>Um código de estado HTTP a indicar o resultado da operação.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var item = await _context.Materiais.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.Materiais.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204 No Content, indicando sucesso.
        }
    }
}