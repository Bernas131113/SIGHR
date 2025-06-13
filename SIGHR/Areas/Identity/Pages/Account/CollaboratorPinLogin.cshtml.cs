// Areas/Identity/Pages/Account/CollaboratorPinLogin.cshtml.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;    // Para ClaimTypes e ClaimsPrincipal
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SIGHR.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class CollaboratorPinLoginModel : PageModel
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<CollaboratorPinLoginModel> _logger;
        private readonly IPasswordHasher<SIGHRUser> _pinHasher; // Injetar o hasher

        public CollaboratorPinLoginModel(
            SIGHRContext context,
            ILogger<CollaboratorPinLoginModel> logger,
            IPasswordHasher<SIGHRUser> pinHasher) // Injetar IPasswordHasher
        {
            _context = context;
            _logger = logger;
            _pinHasher = pinHasher; // Armazenar o hasher
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
            [RegularExpression(@"^\d{4}$", ErrorMessage = "O PIN deve conter exatamente 4 números.")] // Validação de formato
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
                // 1. Encontrar o usuário pelo UserName e Tipos permitidos
                var userToVerify = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == Input.UserName &&
                                              (u.Tipo == "Collaborator" || u.Tipo == "Office" || u.Tipo == "Admin"));
                // Se Admin também pode usar este login, mantenha "Admin".
                // Se este login é SÓ para Collaborator e Office, remova "Admin" daqui.

                if (userToVerify != null && !string.IsNullOrEmpty(userToVerify.PinnedHash))
                {
                    // 2. Verificar o PIN hasheado
                    var pinVerificationResult = _pinHasher.VerifyHashedPassword(
                        userToVerify,
                        userToVerify.PinnedHash,
                        Input.PIN.ToString() // Converte o PIN do formulário para string para verificação
                    );

                    if (pinVerificationResult == PasswordVerificationResult.Success ||
                        pinVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        // PIN Correto
                        var collaboratorUser = userToVerify; // Usuário autenticado
                        _logger.LogInformation("Usuário '{UserName}' (PIN Login) autenticado com sucesso via CollaboratorLoginScheme.", collaboratorUser.UserName);

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, collaboratorUser.UserName!),
                            new Claim(ClaimTypes.NameIdentifier, collaboratorUser.Id),
                            new Claim(ClaimTypes.Role, collaboratorUser.Tipo!) // Adiciona o Tipo do usuário como Role
                        };
                        if (!string.IsNullOrEmpty(collaboratorUser.NomeCompleto))
                        {
                            claims.Add(new Claim("FullName", collaboratorUser.NomeCompleto));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, "CollaboratorLoginScheme");
                        var authProperties = new AuthenticationProperties { IsPersistent = false };

                        await HttpContext.SignInAsync("CollaboratorLoginScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
                        _logger.LogInformation("Usuário '{UserName}' logado com CollaboratorLoginScheme.", collaboratorUser.UserName);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "~/")
                        {
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            // Redirecionar para um dashboard específico do colaborador/office
                            // ou uma página genérica se o tipo não for específico.
                            if (collaboratorUser.Tipo == "Collaborator")
                            {
                                return RedirectToAction("Dashboard", "Collaborator"); // Exemplo
                            }
                            else if (collaboratorUser.Tipo == "Office")
                            {
                                return RedirectToAction("Dashboard", "Office"); // Exemplo
                            }
                            else if (collaboratorUser.Tipo == "Admin" && User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
                            {
                                // Se um admin logou aqui, talvez redirecionar para o painel de admin
                                return RedirectToAction("Index", "Admin");
                            }
                            return RedirectToAction("Index", "Home"); // Fallback
                        }
                    }
                }
                // Se chegou aqui, usuário não encontrado, PIN não configurado, ou PIN incorreto
                _logger.LogWarning("Tentativa de login (PIN) para Colaborador/Office falhou para '{UserName}'. Usuário encontrado: {UserFound}. PIN Hash existe: {PinHashExists}",
                                   Input.UserName,
                                   userToVerify != null,
                                   !string.IsNullOrEmpty(userToVerify?.PinnedHash));
                ModelState.AddModelError(string.Empty, "Nome de utilizador ou PIN inválido ou tipo de utilizador não permitido.");
            }
            this.ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }
    }
}