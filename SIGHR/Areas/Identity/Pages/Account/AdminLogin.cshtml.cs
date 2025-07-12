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
    /// <summary>
    /// PageModel que gere a lógica para a página de login exclusiva de administradores.
    /// Utiliza um esquema de autenticação separado ("AdminLoginScheme") para o PIN.
    /// </summary>
    [AllowAnonymous] // Permite o acesso a esta página sem autenticação.
    public class AdminLoginModel : PageModel
    {
        //
        // Bloco: Injeção de Dependências
        // Serviços essenciais que a página precisa para funcionar.
        //
        private readonly SIGHRContext _context;
        private readonly ILogger<AdminLoginModel> _logger;
        private readonly IPasswordHasher<SIGHRUser> _pinHasher; // Serviço para codificar e verificar o PIN.

        public AdminLoginModel(
            SIGHRContext context,
            ILogger<AdminLoginModel> logger,
            IPasswordHasher<SIGHRUser> pinHasher)
        {
            _context = context;
            _logger = logger;
            _pinHasher = pinHasher;
            Input = new InputModel();
        }

        //
        // Bloco: Propriedades e Modelo de Input
        //

        /// <summary>
        /// Contém os dados submetidos pelo formulário de login.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// A URL para onde o utilizador será redirecionado após um login bem-sucedido.
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Armazena mensagens de erro que persistem entre redirecionamentos.
        /// </summary>
        [TempData]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Define a estrutura e as regras de validação para os campos do formulário.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
            [Display(Name = "Nome de Utilizador")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "O PIN é obrigatório.")]
            [DataType(DataType.Password)]
            [Display(Name = "PIN")]
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
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            ReturnUrl = returnUrl ?? Url.Content("~/");
            // Garante que qualquer cookie de autenticação externa seja limpo.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        /// <summary>
        /// Executado quando o formulário é submetido (POST). Valida os dados e tenta autenticar o utilizador.
        /// </summary>
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Procura um utilizador com o nome de utilizador fornecido e que seja do tipo "Admin".
                var userToVerify = await _context.Users
                                              .FirstOrDefaultAsync(u => u.UserName == Input.UserName &&
                                                                        u.Tipo == "Admin");

                // Verifica se o utilizador foi encontrado e se tem um PIN configurado.
                if (userToVerify != null && !string.IsNullOrEmpty(userToVerify.PinnedHash))
                {
                    // Compara o PIN fornecido com o PIN codificado na base de dados.
                    var pinVerificationResult = _pinHasher.VerifyHashedPassword(
                        userToVerify,
                        userToVerify.PinnedHash,
                        Input.PIN.ToString()
                    );

                    if (pinVerificationResult == PasswordVerificationResult.Success ||
                        pinVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        var adminUser = userToVerify;
                        _logger.LogInformation("Administrador '{UserName}' (Login por PIN) autenticado com sucesso.", adminUser.UserName);

                        // Cria as "claims" (informações) que identificarão o utilizador autenticado.
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

                        // Realiza o login, criando o cookie de autenticação com o esquema "AdminLoginScheme".
                        await HttpContext.SignInAsync("AdminLoginScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
                        _logger.LogInformation("Utilizador '{UserName}' autenticado com o esquema AdminLoginScheme.", adminUser.UserName);

                        // Redireciona para a URL de retorno ou para o dashboard do admin.
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

                // Se chegou aqui, a autenticação falhou.
                _logger.LogWarning("Tentativa de login (PIN) falhou para '{UserName}'. Utilizador encontrado: {UserFound}. Hash do PIN existe: {PinHashExists}",
                                   Input.UserName,
                                   userToVerify != null,
                                   !string.IsNullOrEmpty(userToVerify?.PinnedHash));
                ModelState.AddModelError(string.Empty, "Nome de utilizador ou PIN inválido, ou não é um administrador.");
            }

            // Se o modelo não for válido, reexibe a página com as mensagens de erro.
            this.ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }
    }
}