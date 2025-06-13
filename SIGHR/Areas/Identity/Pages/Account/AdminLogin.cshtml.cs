// Local: Areas/Identity/Pages/Account/AdminLogin.cshtml.cs
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
    public class AdminLoginModel : PageModel
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<AdminLoginModel> _logger;
        private readonly IPasswordHasher<SIGHRUser> _pinHasher; // Injetar o hasher

        public AdminLoginModel(
            SIGHRContext context,
            ILogger<AdminLoginModel> logger,
            IPasswordHasher<SIGHRUser> pinHasher) // Inje��o de depend�ncia
        {
            _context = context;
            _logger = logger;
            _pinHasher = pinHasher;
            Input = new InputModel();
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string? ReturnUrl { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O nome de utilizador � obrigat�rio.")]
            [Display(Name = "Nome de Utilizador")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "O PIN � obrigat�rio.")]
            [DataType(DataType.Password)]
            [Display(Name = "PIN")]
            [RegularExpression(@"^\d{4}$", ErrorMessage = "O PIN deve conter exatamente 4 n�meros.")]
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
                var userToVerify = await _context.Users
                                              .FirstOrDefaultAsync(u => u.UserName == Input.UserName &&
                                                                        u.Tipo == "Admin");

                if (userToVerify != null && !string.IsNullOrEmpty(userToVerify.PinnedHash))
                {
                    var pinVerificationResult = _pinHasher.VerifyHashedPassword(
                        userToVerify,
                        userToVerify.PinnedHash,
                        Input.PIN.ToString()
                    );

                    if (pinVerificationResult == PasswordVerificationResult.Success ||
                        pinVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        var adminUser = userToVerify;
                        _logger.LogInformation("Administrador '{UserName}' (PIN Login) autenticado com sucesso via AdminLoginScheme.", adminUser.UserName);

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, adminUser.UserName!),
                            new Claim(ClaimTypes.NameIdentifier, adminUser.Id),
                            new Claim(ClaimTypes.Role, "Admin")
                        };
                        if (!string.IsNullOrEmpty(adminUser.NomeCompleto))
                        {
                            claims.Add(new Claim("FullName", adminUser.NomeCompleto));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, "AdminLoginScheme");
                        var authProperties = new AuthenticationProperties { IsPersistent = false };

                        await HttpContext.SignInAsync("AdminLoginScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
                        _logger.LogInformation("Usu�rio '{UserName}' logado com AdminLoginScheme.", adminUser.UserName);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "~/")
                        {
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                    }
                }
                // Se chegou aqui, usu�rio n�o encontrado, PIN n�o configurado, ou PIN incorreto
                _logger.LogWarning("Tentativa de login (PIN) falhou para '{UserName}'. Usu�rio encontrado: {UserFound}. PIN Hash existe: {PinHashExists}",
                                   Input.UserName,
                                   userToVerify != null,
                                   !string.IsNullOrEmpty(userToVerify?.PinnedHash));
                ModelState.AddModelError(string.Empty, "Nome de utilizador ou PIN inv�lido, ou n�o � um administrador.");
            }
            this.ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }
    }
}