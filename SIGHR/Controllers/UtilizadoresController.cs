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

[Authorize(Roles = "Admin", AuthenticationSchemes = "AdminLoginScheme")] // Ou o esquema correto para admin
public class UtilizadoresController : Controller
{
    private readonly SIGHRContext _context;
    private readonly UserManager<SIGHRUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPasswordHasher<SIGHRUser> _pinHasher; // INJETAR O HASHER

    public UtilizadoresController(
        SIGHRContext context,
        UserManager<SIGHRUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IPasswordHasher<SIGHRUser> pinHasher) // INJEÇÃO DE DEPENDÊNCIA
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _pinHasher = pinHasher; // ARMAZENAR O HASHER
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
                // PIN não é mais lido diretamente da entidade para exibição.
                // Se você QUISER exibir o PIN (NÃO RECOMENDADO POR SEGURANÇA),
                // você não poderia, pois só temos o hash.
                // Para fins de gestão, o PIN é algo que se define/redefine, não se visualiza.
                // Vou deixar o PIN no ViewModel para o caso de você querer mostrá-lo de alguma forma,
                // mas ele virá do ViewModel de edição, não da entidade.
                // No entanto, para Index, é melhor não mostrar o PIN.
                // PIN = 0, // Ou remova do UtilizadorViewModel se não for para ser exibido
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

        // Carregar dados relacionados, se necessário
        var userWithIncludes = await _context.Users
            .Include(u => u.Horarios)
            .Include(u => u.Faltas)
            .Include(u => u.Encomendas)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (userWithIncludes == null) return NotFound();

        var viewModel = new UtilizadorViewModel
        {
            Id = userWithIncludes.Id,
            UserName = userWithIncludes.UserName ?? string.Empty,
            Email = userWithIncludes.Email ?? string.Empty,
            NomeCompleto = userWithIncludes.NomeCompleto,
            // Não exibir PIN aqui por segurança.
            // PIN = 0, // Se UtilizadorViewModel ainda tiver PIN
            Tipo = userWithIncludes.Tipo,
            Roles = await _userManager.GetRolesAsync(userWithIncludes)
        };
        return View(viewModel);
    }

    // GET: Utilizadores/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync());
        return View(new CreateUtilizadorViewModel()); // ViewModel já tem PIN (int)
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
                NomeCompleto = model.NomeCompleto,
                Tipo = model.Tipo,
                EmailConfirmed = true // Ou false e implementar fluxo de confirmação
            };

            // Hashear o PIN antes de salvar
            if (model.PIN != 0) // Ou alguma outra validação para o PIN do ViewModel
            {
                user.PinnedHash = _pinHasher.HashPassword(user, model.PIN.ToString());
            }

            string dummyPassword = Guid.NewGuid().ToString() + "Aa1" + Guid.NewGuid().ToString().Substring(0, 4) + "!";
            var result = await _userManager.CreateAsync(user, dummyPassword);

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
            // Para o campo PIN no formulário de edição, você pode optar por deixá-lo
            // vazio (indicando que não será alterado) ou pré-preencher com o PIN atual
            // (SE VOCÊ TIVESSE O PIN ORIGINAL, o que não tem mais).
            // Geralmente, para edição de PIN/Senha, você teria campos "Novo PIN" e "Confirmar Novo PIN".
            // Por simplicidade, EditUtilizadorViewModel tem PIN (int). Se for 0, não alteramos.
            PIN = 0, // Ou deixe como está e o usuário pode digitar um novo PIN. O ViewModel já tem PIN (int)
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
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound("Utilizador não encontrado.");

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.NomeCompleto = model.NomeCompleto;
            user.Tipo = model.Tipo;

            // Atualizar PinnedHash apenas se um novo PIN foi fornecido no ViewModel
            // Assumindo que se model.PIN for o valor padrão de int (0), o usuário não quer mudar.
            // Você pode precisar de uma lógica mais explícita (ex: um checkbox "Alterar PIN?")
            if (model.PIN != 0) // Ou if(model.NovoPin.HasValue) se você mudar o ViewModel
            {
                user.PinnedHash = _pinHasher.HashPassword(user, model.PIN.ToString());
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Gerenciar Roles (como antes)
                // ...
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