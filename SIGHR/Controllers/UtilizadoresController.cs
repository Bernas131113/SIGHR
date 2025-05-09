using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGHR.Data;  // Ajuste aqui se o nome do seu DbContext for diferente
using SIGHR.Models;  // Ajuste para o seu namespace correto
using Microsoft.AspNetCore.Authorization;

namespace SIGHR.Controllers
{
    [Authorize] // Apenas pessoas autenticadas terão acesso ao conteúdo do Controller
    public class UtilizadoresController : Controller
    {
        /// <summary>
        /// Referência à Base de Dados do projeto
        /// </summary>
        private readonly ApplicationDbContext _context;

        public UtilizadoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Utilizadores
        public async Task<IActionResult> Index()
        {
            // Procurar na BD todos os utilizadores e listá-los
            // Entregando, de seguida, esses dados à View
            return View(await _context.Utilizadores.ToListAsync());
        }

        // GET: Utilizadores/Details/5
        public async Task<IActionResult> Details(long? id)  // Ajustado o tipo de id para long
        {
            if (id == null)
            {
                return NotFound();
            }

            // Buscar o utilizador por id, incluindo as coleções relacionadas
            var utilizador = await _context.Utilizadores
                .Include(u => u.Horarios)  // Incluir Horarios relacionados
                .Include(u => u.Faltas)    // Incluir Faltas relacionadas
                .Include(u => u.Encomendas) // Incluir Encomendas relacionadas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (utilizador == null)
            {
                return NotFound();
            }

            return View(utilizador);
        }

        // GET: Utilizadores/Edit/5
        public async Task<IActionResult> Edit(long? id)  // Ajustado o tipo de id para long
        {
            if (id == null)
            {
                return NotFound();
            }

            var utilizador = await _context.Utilizadores.FindAsync(id);
            if (utilizador == null)
            {
                return NotFound();
            }
            return View(utilizador);
        }

        // POST: Utilizadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Nome,Username,PIN,Tipo")] Utilizadores utilizador)
        {
            if (id != utilizador.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(utilizador);  // Atualizar o utilizador na base de dados
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UtilizadoresExists(utilizador.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));  // Redirecionar para a lista de utilizadores
            }
            return View(utilizador);
        }

        // GET: Utilizadores/Delete/5
        public async Task<IActionResult> Delete(long? id)  // Ajustado o tipo de id para long
        {
            if (id == null)
            {
                return NotFound();
            }

            var utilizador = await _context.Utilizadores
                .FirstOrDefaultAsync(m => m.Id == id);
            if (utilizador == null)
            {
                return NotFound();
            }

            return View(utilizador);
        }

        // POST: Utilizadores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)  // Ajustado o tipo de id para long
        {
            var utilizador = await _context.Utilizadores.FindAsync(id);
            if (utilizador != null)
            {
                _context.Utilizadores.Remove(utilizador);  // Remover o utilizador da base de dados
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));  // Redirecionar para a lista de utilizadores
        }

        private bool UtilizadoresExists(long id)  // Ajustado o tipo de id para long
        {
            return _context.Utilizadores.Any(e => e.Id == id);  // Verificar se o utilizador existe
        }
    }
}
