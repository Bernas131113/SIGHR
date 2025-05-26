// Areas/Identity/Pages/Account/AdminLogin.cshtml.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;       // Para IdentityConstants
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SIGHR.Areas.Identity.Data;      // Namespace para SIGHRContext e SIGHRUser
using System.Collections.Generic;       // Para List<Claim>
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;     // Para ILogger

namespace SIGHR.Areas.Identity.Pages.Account
{
    [AllowAnonymous] // Permite acesso an�nimo a esta p�gina de login
    public class AdminLoginModel : PageModel
    {
        private readonly SIGHRContext _context;
        private readonly ILogger<AdminLoginModel> _logger;

        public AdminLoginModel(
            SIGHRContext context,
            ILogger<AdminLoginModel> logger)
        {
            _context = context;
            _logger = logger;
            Input = new InputModel();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        // Esta propriedade ReturnUrl � usada para passar o returnUrl de volta para a view
        // no caso de falha de valida��o do modelo ou falha de login, para que um campo
        // oculto no formul�rio possa reenvi�-lo.
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
            public int PIN { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            // Armazena o returnUrl para ser usado se a p�gina for reexibida
            // ou para ser inclu�do como um par�metro de rota no POST do formul�rio
            ReturnUrl = returnUrl ?? Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null) // Este 'returnUrl' vem do formul�rio/query string
        {
            // A propriedade this.ReturnUrl � mais para re-popular a view se o POST falhar.
            // Para a l�gica de redirecionamento p�s-login, use o par�metro 'returnUrl' do m�todo.

            if (ModelState.IsValid)
            {
                var adminUser = await _context.Users
                                              .FirstOrDefaultAsync(u => u.UserName == Input.UserName &&
                                                                        u.PIN == Input.PIN &&
                                                                        u.Tipo == "Admin");

                if (adminUser != null)
                {
                    _logger.LogInformation($"Administrador '{adminUser.UserName}' (PIN Login) autenticado com sucesso via AdminLoginScheme.");

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, adminUser.UserName!),
                        new Claim(ClaimTypes.NameIdentifier, adminUser.Id),
                        new Claim(ClaimTypes.Role, "Admin") // Adiciona a claim de Role diretamente
                    };

                    if (!string.IsNullOrEmpty(adminUser.NomeCompleto))
                    {
                        claims.Add(new Claim("FullName", adminUser.NomeCompleto));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, "AdminLoginScheme");
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false, // Mude para true se tiver um checkbox "Lembrar-me"
                                              // Outras propriedades como ExpiresUtc s�o geralmente definidas pelo esquema no Program.cs
                    };

                    await HttpContext.SignInAsync(
                        "AdminLoginScheme",
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation($"Usu�rio '{adminUser.UserName}' logado com o esquema 'AdminLoginScheme'. Decidindo redirecionamento...");

                    // L�gica de Redirecionamento Aprimorada:
                    // Usa o 'returnUrl' que foi passado para esta action OnPostAsync.
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "~/")
                    {
                        _logger.LogInformation($"Redirecionando para ReturnUrl da requisi��o: {returnUrl}");
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        // Se n�o h� um ReturnUrl v�lido e espec�fico da requisi��o,
                        // SEMPRE redireciona para a p�gina de �ndice do admin.
                        _logger.LogInformation("ReturnUrl da requisi��o inv�lido, ausente ou raiz. Redirecionando para Admin/Index.");
                        return RedirectToAction("Index", "Admin");
                    }
                }
                else
                {
                    _logger.LogWarning($"Tentativa de login (PIN) falhou para o utilizador: {Input.UserName}.");
                    ModelState.AddModelError(string.Empty, "Nome de utilizador ou PIN inv�lido, ou n�o � um administrador.");
                }
            }

            // Se ModelState n�o � v�lido, ou falha de login, re-popular this.ReturnUrl para a view
            // para que o formul�rio possa manter o contexto do returnUrl original (se houver).
            this.ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }
    }
}