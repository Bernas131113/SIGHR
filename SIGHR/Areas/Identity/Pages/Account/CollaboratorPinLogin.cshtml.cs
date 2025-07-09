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
    public class CollaboratorPinLoginModel : PageModel
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<CollaboratorPinLoginModel> _logger;
        private readonly IPasswordHasher<SIGHRUser> _pinHasher;

        public CollaboratorPinLoginModel(SIGHRContext context, ILogger<CollaboratorPinLoginModel> logger, IPasswordHasher<SIGHRUser> pinHasher)
        {
            _context = context;
            _logger = logger;
            _pinHasher = pinHasher;
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
            public string UserName { get; set; } = string.Empty;
            [Required(ErrorMessage = "O PIN é obrigatório.")]
            [DataType(DataType.Password)]
            [RegularExpression(@"^\d{4}$", ErrorMessage = "O PIN deve conter exatamente 4 números.")]
            public int PIN { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage)) ModelState.AddModelError(string.Empty, ErrorMessage);
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Permite que Admin, Office, ou Collaborator usem esta página de login por PIN
                var userToVerify = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == Input.UserName &&
                                              (u.Tipo == "Collaborator" || u.Tipo == "Admin"));

                if (userToVerify != null && !string.IsNullOrEmpty(userToVerify.PinnedHash))
                {
                    var pinVerificationResult = _pinHasher.VerifyHashedPassword(userToVerify, userToVerify.PinnedHash, Input.PIN.ToString());

                    if (pinVerificationResult == PasswordVerificationResult.Success || pinVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        var loggedInUser = userToVerify;
                        _logger.LogInformation("Usuário '{UserName}' (Tipo: {UserType}, PIN Login) autenticado com sucesso via CollaboratorLoginScheme.", loggedInUser.UserName, loggedInUser.Tipo);

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, loggedInUser.UserName!),
                            new Claim(ClaimTypes.NameIdentifier, loggedInUser.Id),
                            new Claim(ClaimTypes.Role, loggedInUser.Tipo!) // Usa o Tipo do usuário como Role
                        };
                        if (!string.IsNullOrEmpty(loggedInUser.NomeCompleto))
                        {
                            claims.Add(new Claim("FullName", loggedInUser.NomeCompleto));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, "CollaboratorLoginScheme");
                        var authProperties = new AuthenticationProperties { IsPersistent = false };
                        await HttpContext.SignInAsync("CollaboratorLoginScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
                        _logger.LogInformation("Usuário '{UserName}' logado com CollaboratorLoginScheme.", loggedInUser.UserName);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "~/")
                        {
                            _logger.LogInformation("Redirecionando para ReturnUrl da requisição: {ReturnUrl}", returnUrl);
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            // ****** MUDANÇA PRINCIPAL AQUI ******
                            // Se logou por esta página, SEMPRE vai para o dashboard do colaborador,
                            // independentemente do 'Tipo' do usuário.
                            // A autorização NAS PÁGINAS do dashboard do colaborador é que decidirá
                            // se um Admin (que logou aqui) pode realmente ver/fazer coisas lá.
                            _logger.LogInformation("ReturnUrl padrão. Redirecionando para Collaborator/Dashboard.");
                            return RedirectToAction("Dashboard", "Collaborator"); // Assumindo que existe CollaboratorController com Dashboard action
                        }
                    }
                }
                _logger.LogWarning("Tentativa de login (PIN Colaborador) falhou para '{UserName}'.", Input.UserName);
                ModelState.AddModelError(string.Empty, "Nome de utilizador ou PIN inválido, ou tipo de utilizador não permitido para este login.");
            }
            this.ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }
    }
}