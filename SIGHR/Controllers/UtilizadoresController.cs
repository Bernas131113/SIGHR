// Controllers/UtilizadoresController.cs
namespace SIGHR.Controllers; // Usando namespace com escopo de arquivo

// Usings devem vir DEPOIS da declaração de namespace com escopo de arquivo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data; // Para SIGHRContext e SIGHRUser
using SIGHR.Models.ViewModels; // Para seus ViewModels

[Authorize(Roles = "Admin")] // Apenas usuários com o Role "Admin" podem acessar
public class UtilizadoresController : Controller
{
    private readonly SIGHRContext _context;
    private readonly UserManager<SIGHRUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UtilizadoresController(
        SIGHRContext context,
        UserManager<SIGHRUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Utilizadores
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UtilizadorViewModel>();
        foreach (var user in users)
        {
            userViewModels.Add(new UtilizadorViewModel
            {
                Id = user.Id, // user.Id é string (não nulo), UtilizadorViewModel.Id é 'required string'
                UserName = user.UserName, // UserName pode ser nulo em IdentityUser, mas UserManager geralmente garante que não.
                Email = user.Email,       // Email pode ser nulo
                NomeCompleto = user.NomeCompleto, // string?
                PIN = user.PIN,
                Tipo = user.Tipo, // string?
                Roles = await _userManager.GetRolesAsync(user) // Obtém a lista de roles
            });
        }
        return View(userViewModels);
    }

    // GET: Utilizadores/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Carregar dados relacionados explicitamente se necessário para a view Details
        // Se as coleções (Horarios, etc.) estão em SIGHRUser e você quer exibi-las:
        var userWithIncludes = await _context.Users
            .Include(u => u.Horarios) // Exemplo
            .Include(u => u.Faltas)   // Exemplo
            .Include(u => u.Encomendas) // Exemplo
            .FirstOrDefaultAsync(u => u.Id == id);

        if (userWithIncludes == null) // Verificação extra, embora user já tenha sido encontrado
        {
            return NotFound();
        }

        var viewModel = new UtilizadorViewModel
        {
            Id = userWithIncludes.Id,
            UserName = userWithIncludes.UserName,
            Email = userWithIncludes.Email,
            NomeCompleto = userWithIncludes.NomeCompleto,
            PIN = userWithIncludes.PIN,
            Tipo = userWithIncludes.Tipo,
            Roles = await _userManager.GetRolesAsync(userWithIncludes)
            // Se você adicionou as coleções ao UtilizadorViewModel:
            // Horarios = userWithIncludes.Horarios,
            // Faltas = userWithIncludes.Faltas,
            // Encomendas = userWithIncludes.Encomendas
        };

        return View(viewModel);
    }

    // GET: Utilizadores/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync());
        return View(new CreateUtilizadorViewModel());
    }

    // POST: Utilizadores/Create
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
                NomeCompleto = model.NomeCompleto, // model.NomeCompleto é string?
                PIN = model.PIN,
                Tipo = model.Tipo,                 // model.Tipo é string?
                EmailConfirmed = true              // Considere a necessidade de confirmação de email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Tipo) && await _roleManager.RoleExistsAsync(model.Tipo))
                {
                    await _userManager.AddToRoleAsync(user, model.Tipo);
                }
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync(), model.Tipo);
        return View(model);
    }

    // GET: Utilizadores/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var model = new EditUtilizadorViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty, // Garante que não é nulo para o ViewModel se UserName fosse string
            Email = user.Email ?? string.Empty,       // Garante que não é nulo para o ViewModel se Email fosse string
            NomeCompleto = user.NomeCompleto, // string? é OK
            PIN = user.PIN,
            Tipo = user.Tipo ?? userRoles.FirstOrDefault() // string? é OK, ou pega o primeiro role
        };

        ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync(), model.Tipo);
        return View(model);
    }

    // POST: Utilizadores/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUtilizadorViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound("Utilizador não encontrado.");
            }

            // Atualizar propriedades
            // Se UserName é usado para login, mudar pode ter implicações.
            // Identity normaliza UserName e Email para maiúsculas.
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.NomeCompleto = model.NomeCompleto; // model.NomeCompleto é string? user.NomeCompleto é string?
            user.PIN = model.PIN;
            user.Tipo = model.Tipo;                 // model.Tipo é string? user.Tipo é string?

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Gerenciar Roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                // Remover todos os roles atuais (exceto o novo, se for o mesmo)
                foreach (var role in currentRoles)
                {
                    if (role != model.Tipo) // Não remove se o tipo/role selecionado for o mesmo que um existente
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }
                }
                // Adicionar ao novo role, se especificado, válido e ainda não atribuído
                if (!string.IsNullOrEmpty(model.Tipo) &&
                    await _roleManager.RoleExistsAsync(model.Tipo) &&
                    !await _userManager.IsInRoleAsync(user, model.Tipo))
                {
                    await _userManager.AddToRoleAsync(user, model.Tipo);
                }

                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync(), model.Tipo);
        return View(model);
    }

    // GET: Utilizadores/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
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

    // POST: Utilizadores/Delete/5
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
                // Adicionar erros ao ModelState para serem exibidos na view de Delete
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                // Re-popular o ViewModel e retornar para a view de Delete
                var viewModel = new UtilizadorViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    NomeCompleto = user.NomeCompleto,
                    Tipo = user.Tipo,
                    Roles = await _userManager.GetRolesAsync(user) // Pode ser útil para a view de erro
                };
                return View("Delete", viewModel);
            }
        }
        // Se o usuário não foi encontrado ou a exclusão foi bem-sucedida
        return RedirectToAction(nameof(Index));
    }
}