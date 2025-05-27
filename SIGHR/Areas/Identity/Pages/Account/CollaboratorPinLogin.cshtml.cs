// Areas/Identity/Pages/Account/CollaboratorPinLogin.cshtml.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class CollaboratorPinLoginModel : PageModel // <<< NOME DA CLASSE ALTERADO
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<CollaboratorPinLoginModel> _logger; // <<< TIPO DO LOGGER ALTERADO

        public CollaboratorPinLoginModel(SIGHRContext context, ILogger<CollaboratorPinLoginModel> logger) // <<< CONSTRUTOR ALTERADO
        {
            _context = context;
            _logger = logger;
            Input = new InputBindingModel();
        }

        [BindProperty]
        public InputBindingModel Input { get; set; }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputBindingModel
        {
            [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
            [Display(Name = "Nome de Utilizador")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "O PIN é obrigatório.")]
            [DataType(DataType.Password)]
            [Display(Name = "PIN")]
            public int PIN { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var collaboratorUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == Input.UserName &&
                                              u.PIN == Input.PIN &&
                                              (u.Tipo == "Collaborator" || u.Tipo == "Office" || u.Tipo == "Admin"));

                if (collaboratorUser != null)
                {
                    _logger.LogInformation($"Colaborador '{collaboratorUser.UserName}' (PIN Login) autenticado com sucesso via CollaboratorLoginScheme.");
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, collaboratorUser.UserName!),
                        new Claim(ClaimTypes.NameIdentifier, collaboratorUser.Id),
                        new Claim(ClaimTypes.Role, collaboratorUser.Tipo!)
                    };
                    if (!string.IsNullOrEmpty(collaboratorUser.NomeCompleto))
                    {
                        claims.Add(new Claim("FullName", collaboratorUser.NomeCompleto));
                    }
                    var claimsIdentity = new ClaimsIdentity(claims, "CollaboratorLoginScheme");
                    var authProperties = new AuthenticationProperties { IsPersistent = false };
                    await HttpContext.SignInAsync("CollaboratorLoginScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
                    _logger.LogInformation($"Usuário '{collaboratorUser.UserName}' logado com CollaboratorLoginScheme.");
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "~/")
                    {
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Dashboard", "Collaborator");
                    }
                }
                else
                {
                    _logger.LogWarning($"Tentativa de login (PIN) de colaborador falhou para: {Input.UserName}.");
                    ModelState.AddModelError(string.Empty, "Nome de utilizador ou PIN inválido ou tipo de utilizador não permitido.");
                }
            }
            this.ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }
    }
}