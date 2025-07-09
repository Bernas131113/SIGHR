// Controllers/UtilizadoresController.cs
namespace SIGHR.Controllers;

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

[Authorize(Policy = "AdminAccessUI")]
public class UtilizadoresController : Controller
{
    private readonly SIGHRContext _context;
    private readonly UserManager<SIGHRUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPasswordHasher<SIGHRUser> _pinHasher;
    private readonly ILogger<UtilizadoresController> _logger; // Campo para o logger
    public UtilizadoresController(
        SIGHRContext context,
        UserManager<SIGHRUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IPasswordHasher<SIGHRUser> pinHasher,
        ILogger<UtilizadoresController> logger) // Injeção de dependência para ILogger
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _pinHasher = pinHasher;
        _logger = logger; // Armazena o logger injetado
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
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                NomeCompleto = user.NomeCompleto,
                Tipo = user.Tipo,
                Roles = await _userManager.GetRolesAsync(user)
            });
        }
        return View(userViewModels);
    }

    // GET: Utilizadores/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var viewModel = new UtilizadorViewModel { Id = user.Id, UserName = user.UserName, Email = user.Email, NomeCompleto = user.NomeCompleto, Tipo = user.Tipo, Roles = await _userManager.GetRolesAsync(user) };
        return View(viewModel);
    }

    // GET: Utilizadores/Create
    public async Task<IActionResult> Create()
    {
        // Filtra os roles para não incluir "Office" se ele ainda existir no banco de dados por algum motivo
        ViewBag.Roles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Office").Select(r => r.Name).ToListAsync());
        return View(new CreateUtilizadorViewModel());
    }
    // POST: Utilizadores/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUtilizadorViewModel model)
    {
        // Removido o campo de senha do ViewModel, gerando uma senha dummy
        if (ModelState.IsValid)
        {
            var user = new SIGHRUser { UserName = model.UserName, Email = model.Email, NomeCompleto = model.NomeCompleto, Tipo = model.Tipo, EmailConfirmed = true };
            if (model.PIN != 0)
            {
                user.PinnedHash = _pinHasher.HashPassword(user, model.PIN.ToString());
            }
            string dummyPassword = Guid.NewGuid().ToString() + "P@ss1!";
            var result = await _userManager.CreateAsync(user, dummyPassword);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Tipo) && await _roleManager.RoleExistsAsync(model.Tipo))
                {
                    await _userManager.AddToRoleAsync(user, model.Tipo);
                }
                _logger.LogInformation("Utilizador '{UserName}' criado com sucesso pelo administrador.", user.UserName);
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        }
        ViewBag.Roles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Office").Select(r => r.Name).ToListAsync(), model.Tipo);
        return View(model);
    }

    // GET: Utilizadores/Edit/5
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
            PIN = 0, // PIN começa vazio, para ser alterado opcionalmente
            Tipo = user.Tipo ?? userRoles.FirstOrDefault()
        };
        ViewBag.Roles = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Office").Select(r => r.Name).ToListAsync(), model.Tipo);
        return View(model);
    }

    // POST: Utilizadores/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUtilizadorViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound("Utilizador não encontrado.");

            user.UserName = model.UserName; user.Email = model.Email;
            user.NomeCompleto = model.NomeCompleto; user.Tipo = model.Tipo;
            if (model.PIN != 0) user.PinnedHash = _pinHasher.HashPassword(user, model.PIN.ToString());

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
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

    // GET: Utilizadores/Delete/5
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
            // PIN = 0, // Não mostrar PIN na confirmação de exclusão
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
                // ... (tratamento de erro e repopular ViewModel como antes) ...
            }
        }
        return RedirectToAction(nameof(Index));
    }
}