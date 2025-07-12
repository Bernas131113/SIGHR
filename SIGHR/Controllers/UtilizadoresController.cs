// Controllers/UtilizadoresController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace SIGHR.Controllers
{
    /// <summary>
    /// Controlador para a gestão completa de utilizadores (CRUD - Criar, Ler, Atualizar, Apagar).
    /// Acesso restrito a administradores através da política "AdminAccessUI".
    /// </summary>
    [Authorize(Policy = "AdminAccessUI")]
    public class UtilizadoresController : Controller
    {
        //
        // Bloco: Injeção de Dependências
        //
        private readonly SIGHRContext _context;
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IPasswordHasher<SIGHRUser> _pinHasher;
        private readonly ILogger<UtilizadoresController> _logger;

        public UtilizadoresController(
            SIGHRContext context,
            UserManager<SIGHRUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IPasswordHasher<SIGHRUser> pinHasher,
            ILogger<UtilizadoresController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _pinHasher = pinHasher;
            _logger = logger;
        }

        //
        // Bloco: Leitura (Read) de Utilizadores
        //

        /// <summary>
        /// Action para listar todos os utilizadores existentes no sistema.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UtilizadorViewModel>();
            foreach (var user in users)
            {
                userViewModels.Add(new UtilizadorViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    NomeCompleto = user.NomeCompleto,
                    Tipo = user.Tipo,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }
            return View(userViewModels);
        }

        /// <summary>
        /// Action para exibir os detalhes de um utilizador específico.
        /// </summary>
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var viewModel = new UtilizadorViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                NomeCompleto = user.NomeCompleto,
                Tipo = user.Tipo,
                Roles = await _userManager.GetRolesAsync(user)
            };
            return View(viewModel);
        }

        //
        // Bloco: Criação (Create) de Utilizadores
        //

        /// <summary>
        /// Apresenta o formulário para criar um novo utilizador.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            // Prepara a lista de funções (Roles) para o dropdown, excluindo lixo antigo se existir.
            ViewBag.Roles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Office").Select(r => r.Name).ToListAsync());
            return View(new CreateUtilizadorViewModel());
        }

        /// <summary>
        /// Processa a submissão do formulário de criação de um novo utilizador.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUtilizadorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new SIGHRUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    NomeCompleto = model.NomeCompleto,
                    Tipo = model.Tipo,
                    EmailConfirmed = true // Confirma o email automaticamente.
                };

                // Se um PIN foi fornecido, gera o seu hash.
                if (model.PIN != 0)
                {
                    user.PinnedHash = _pinHasher.HashPassword(user, model.PIN.ToString());
                }

                // Gera uma palavra-passe complexa e aleatória, pois não é necessária para o login por PIN.
                string dummyPassword = Guid.NewGuid().ToString() + "P@ss1!";
                var result = await _userManager.CreateAsync(user, dummyPassword);

                if (result.Succeeded)
                {
                    // Se o utilizador foi criado com sucesso, atribui-lhe a função (Role) selecionada.
                    if (!string.IsNullOrEmpty(model.Tipo) && await _roleManager.RoleExistsAsync(model.Tipo))
                    {
                        await _userManager.AddToRoleAsync(user, model.Tipo);
                    }
                    _logger.LogInformation("Utilizador '{UserName}' criado com sucesso pelo administrador.", user.UserName);
                    return RedirectToAction(nameof(Index));
                }
                // Se houver erros, adiciona-os ao ModelState para serem exibidos na View.
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            }

            // Se o modelo não for válido, recarrega o formulário com os dados e erros.
            ViewBag.Roles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Office").Select(r => r.Name).ToListAsync(), model.Tipo);
            return View(model);
        }

        //
        // Bloco: Edição (Update) de Utilizadores
        //

        /// <summary>
        /// Apresenta o formulário para editar um utilizador existente.
        /// </summary>
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUtilizadorViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                NomeCompleto = user.NomeCompleto,
                PIN = 0, // O campo PIN começa vazio; só é alterado se um novo valor for inserido.
                Tipo = user.Tipo ?? userRoles.FirstOrDefault()
            };
            ViewBag.Roles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Office").Select(r => r.Name).ToListAsync(), model.Tipo);
            return View(model);
        }

        /// <summary>
        /// Processa a submissão do formulário de edição de um utilizador.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUtilizadorViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null) return NotFound("Utilizador não encontrado.");

                // Atualiza as propriedades do utilizador.
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.NomeCompleto = model.NomeCompleto;
                user.Tipo = model.Tipo;
                if (model.PIN != 0) user.PinnedHash = _pinHasher.HashPassword(user, model.PIN.ToString());

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Sincroniza a função (Role) do utilizador.
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!string.IsNullOrEmpty(model.Tipo) && await _roleManager.RoleExistsAsync(model.Tipo))
                    {
                        await _userManager.AddToRoleAsync(user, model.Tipo);
                    }
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            }
            ViewBag.Roles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Office").Select(r => r.Name).ToListAsync(), model.Tipo);
            return View(model);
        }

        //
        // Bloco: Exclusão (Delete) de Utilizadores
        //

        /// <summary>
        /// Apresenta a página de confirmação para excluir um utilizador.
        /// </summary>
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var viewModel = new UtilizadorViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                NomeCompleto = user.NomeCompleto,
                Tipo = user.Tipo,
                Roles = await _userManager.GetRolesAsync(user)
            };
            return View(viewModel);
        }

        /// <summary>
        /// Confirma e executa a exclusão de um utilizador.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    // Se a exclusão falhar (ex: por causa de restrições de BD), pode-se adicionar um erro.
                    ModelState.AddModelError("", "Ocorreu um erro ao excluir o utilizador.");
                    // Idealmente, retornaria à página de confirmação com a mensagem de erro.
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}