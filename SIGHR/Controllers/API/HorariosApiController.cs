using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models;

namespace SIGHR.Controllers.Api
{
    /// <summary>
    /// Controlador de API para gerir as operações CRUD (Criar, Ler, Atualizar, Apagar) da entidade Horario.
    /// Define os endpoints que podem ser chamados por clientes HTTP (como JavaScript).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HorariosApiController : ControllerBase
    {
        // O contexto da base de dados, injetado no construtor.
        private readonly SIGHRContext _context;

        public HorariosApiController(SIGHRContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Endpoint para obter a lista de todos os horários.
        /// Rota: GET api/HorariosApi
        /// </summary>
        /// <returns>Uma lista de objetos do tipo <see cref="Horario"/>.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Horario>>> Get()
        {
            return await _context.Horarios.ToListAsync();
        }

        /// <summary>
        /// Endpoint para obter um horário específico através do seu ID.
        /// Rota: GET api/HorariosApi/{id}
        /// </summary>
        /// <param name="id">O ID do horário a ser procurado.</param>
        /// <returns>O objeto <see cref="Horario"/> correspondente ao ID, ou 'NotFound' (404) se não existir.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Horario>> Get(long id)
        {
            var item = await _context.Horarios.FindAsync(id);
            return item == null ? NotFound() : item;
        }

        /// <summary>
        /// Endpoint para atualizar um horário existente.
        /// Rota: PUT api/HorariosApi/{id}
        /// </summary>
        /// <param name="id">O ID do horário a ser atualizado.</param>
        /// <param name="model">O objeto Horario com os dados atualizados.</param>
        /// <returns>Um código de estado HTTP a indicar o resultado da operação.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, Horario model)
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
                // Verifica se o horário ainda existe, para evitar erros de concorrência.
                if (!_context.Horarios.Any(e => e.Id == id))
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
        /// Endpoint para criar um novo horário.
        /// Rota: POST api/HorariosApi
        /// </summary>
        /// <param name="model">O objeto Horario com os dados a serem criados.</param>
        /// <returns>O horário recém-criado, com o seu novo ID e um código de estado 201 Created.</returns>
        [HttpPost]
        public async Task<ActionResult<Horario>> Post(Horario model)
        {
            _context.Horarios.Add(model);
            await _context.SaveChangesAsync();

            // Retorna o horário criado e a URL para o aceder (boa prática REST).
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        /// <summary>
        /// Endpoint para remover um horário existente.
        /// Rota: DELETE api/HorariosApi/{id}
        /// </summary>
        /// <param name="id">O ID do horário a ser removido.</param>
        /// <returns>Um código de estado HTTP a indicar o resultado da operação.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var item = await _context.Horarios.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.Horarios.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204 No Content, indicando sucesso.
        }
    }
}