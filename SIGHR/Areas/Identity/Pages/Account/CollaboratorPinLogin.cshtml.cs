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
    /// <summary>
    /// PageModel que gere a lógica para a página de login de colaboradores através de PIN.
    /// Utiliza um esquema de autenticação separado ("CollaboratorLoginScheme").
    /// </summary>
    [AllowAnonymous] // Permite o acesso a esta página sem autenticação.
    public class CollaboratorPinLoginModel : PageModel
    {
        //
        // Bloco: Injeção de Dependências
        //
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

        //
        // Bloco: Propriedades e Modelo de Input
        //
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

        //
        // Bloco: Manipuladores de Pedidos HTTP (Handlers)
        //

        /// <summary>
        /// Executado quando a página é acedida via GET. Prepara a página para ser exibida.
        /// </summary>
        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage)) ModelState.AddModelError(string.Empty, ErrorMessage);
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        /// <summary>
        /// Executado quando o formulário é submetido (POST). Valida os dados e tenta autenticar o utilizador.
        /// </summary>
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Procura um utilizador com o nome de utilizador fornecido que seja do tipo "Collaborator" ou "Admin".
                var userToVerify = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == Input.UserName &&
                                              (u.Tipo == "Collaborator" || u.Tipo == "Admin"));

                if (userToVerify != null && !string.IsNullOrEmpty(userToVerify.PinnedHash))
                {
                    // Compara o PIN fornecido com o PIN codificado na base de dados.
                    var pinVerificationResult = _pinHasher.VerifyHashedPassword(userToVerify, userToVerify.PinnedHash, Input.PIN.ToString());

                    if (pinVerificationResult == PasswordVerificationResult.Success || pinVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        var loggedInUser = userToVerify;
                        _logger.LogInformation("Utilizador '{UserName}' (Tipo: {UserType}, Login por PIN) autenticado com sucesso via CollaboratorLoginScheme.", loggedInUser.UserName, loggedInUser.Tipo);

                        // Cria as "claims" (informações) que identificarão o utilizador autenticado.
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, loggedInUser.UserName!),
                            new Claim(ClaimTypes.NameIdentifier, loggedInUser.Id),
                            new Claim(ClaimTypes.Role, loggedInUser.Tipo!) // Usa o Tipo do utilizador como a sua Função (Role).
                        };
                        if (!string.IsNullOrEmpty(loggedInUser.NomeCompleto))
                        {
                            claims.Add(new Claim("FullName", loggedInUser.NomeCompleto));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, "CollaboratorLoginScheme");
                        var authProperties = new AuthenticationProperties { IsPersistent = false };

                        // Realiza o login, criando o cookie de autenticação com o esquema "CollaboratorLoginScheme".
                        await HttpContext.SignInAsync("CollaboratorLoginScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
                        _logger.LogInformation("Utilizador '{UserName}' autenticado com o esquema CollaboratorLoginScheme.", loggedInUser.UserName);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "~/")
                        {
                            _logger.LogInformation("A redirecionar para a ReturnUrl do pedido: {ReturnUrl}", returnUrl);
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            // Independentemente do tipo, se o login for feito nesta página, o destino é o dashboard do colaborador.
                            _logger.LogInformation("ReturnUrl padrão. A redirecionar para Collaborator/Dashboard.");
                            return RedirectToAction("Dashboard", "Collaborator");
                        }
                    }
                }

                // Se a autenticação falhou, regista o aviso e adiciona um erro ao modelo.
                _logger.LogWarning("Tentativa de login (PIN Colaborador) falhou para '{UserName}'.", Input.UserName);
                ModelState.AddModelError(string.Empty, "Nome de utilizador ou PIN inválido, ou tipo de utilizador não permitido para este login.");
            }

            // Se o modelo não for válido, reexibe a página com as mensagens de erro.
            this.ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }
    }
}