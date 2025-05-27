// Controllers/UtilizadoresController.cs
namespace SIGHR.Controllers;

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
using SIGHR.Areas.Identity.Data;
using SIGHR.Models.ViewModels; // Para seus ViewModels

// CORREÇÃO PRINCIPAL AQUI:
[Authorize(Roles = "Admin", AuthenticationSchemes = "AdminLoginScheme")]
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
                Id = user.Id,
                UserName = user.UserName ?? string.Empty, // Garante não nulo para ViewModel se necessário
                Email = user.Email ?? string.Empty,       // Garante não nulo para ViewModel se necessário
                NomeCompleto = user.NomeCompleto,         // string? é ok se ViewModel também é string?
                PIN = user.PIN,
                Tipo = user.Tipo,                         // string? é ok se ViewModel também é string?
                Roles = await _userManager.GetRolesAsync(user)
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

        var userWithIncludes = await _context.Users
            .Include(u => u.Horarios)
            .Include(u => u.Faltas)
            .Include(u => u.Encomendas)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (userWithIncludes == null)
        {
            return NotFound(); // Embora improvável se user foi encontrado acima
        }

        var viewModel = new UtilizadorViewModel
        {
            Id = userWithIncludes.Id,
            UserName = userWithIncludes.UserName ?? string.Empty,
            Email = userWithIncludes.Email ?? string.Empty,
            NomeCompleto = userWithIncludes.NomeCompleto,
            PIN = userWithIncludes.PIN,
            Tipo = userWithIncludes.Tipo,
            Roles = await _userManager.GetRolesAsync(userWithIncludes)
            // Horarios = userWithIncludes.Horarios, // Descomente se adicionado ao ViewModel
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
    public async Task<IActionResult> Create(CreateUtilizadorViewModel model) // Agora usa o ViewModel sem senha
    {
        if (ModelState.IsValid)
        {
            var user = new SIGHRUser
            {
                UserName = model.UserName,
                Email = model.Email,
                NomeCompleto = model.NomeCompleto,
                PIN = model.PIN,
                Tipo = model.Tipo, // Esta será a propriedade 'Tipo' no SIGHRUser e também o nome do Role do Identity
                EmailConfirmed = true // Defina como true se não houver fluxo de confirmação de email
            };

            // Gerar uma senha dummy forte e aleatória, pois CreateAsync requer uma.
            // Esta senha não será usada pelo usuário se ele logar via PIN.
            string dummyPassword = Guid.NewGuid().ToString() + "Aa1" + Guid.NewGuid().ToString().Substring(0, 4) + "!";

            var result = await _userManager.CreateAsync(user, dummyPassword);
            if (result.Succeeded)
            {
                // Adicionar ao Role do Identity baseado na propriedade Tipo/Role do ViewModel
                if (!string.IsNullOrEmpty(model.Tipo) && await _roleManager.RoleExistsAsync(model.Tipo))
                {
                    await _userManager.AddToRoleAsync(user, model.Tipo);
                }
                else if (!string.IsNullOrEmpty(model.Tipo))
                {
                    // Opcional: Criar o role se ele não existir E você quiser essa funcionalidade
                    // var roleResult = await _roleManager.CreateAsync(new IdentityRole(model.Tipo));
                    // if (roleResult.Succeeded)
                    // {
                    //    await _userManager.AddToRoleAsync(user, model.Tipo);
                    // }
                    // else
                    // {
                    //    // Logar erro na criação do role
                    // }
                    ModelState.AddModelError("Tipo", $"O Role '{model.Tipo}' não existe. Crie-o primeiro ou selecione um existente.");
                    // Re-popular ViewBag.Roles antes de retornar a view com erro
                    ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync(), model.Tipo);
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        // Se ModelState inválido, re-popular ViewBag.Roles
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
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            NomeCompleto = user.NomeCompleto,
            PIN = user.PIN,
            Tipo = user.Tipo ?? userRoles.FirstOrDefault()
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

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.NomeCompleto = model.NomeCompleto;
            user.PIN = model.PIN;
            user.Tipo = model.Tipo;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                foreach (var role in currentRoles)
                {
                    if (role != model.Tipo)
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }
                }
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
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
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
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                var viewModel = new UtilizadorViewModel // Repopular para a view de erro
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    NomeCompleto = user.NomeCompleto,
                    Tipo = user.Tipo,
                    Roles = await _userManager.GetRolesAsync(user)
                };
                return View("Delete", viewModel);
            }
        }
        return RedirectToAction(nameof(Index));
    }
}