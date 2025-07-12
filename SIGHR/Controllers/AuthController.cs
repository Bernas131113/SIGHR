// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models.ViewModels; // Onde LoginApiViewModel está definido
using SIGHR.Services;          // Onde TokenService está definido
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SIGHR.Controllers
{
    /// <summary>
    /// Controlador de API responsável pela autenticação de utilizadores.
    /// Gere o processo de login e a emissão de tokens de acesso (JWT).
    /// </summary>
    [Route("api/[controller]")] // Define a rota base como /api/Auth
    [ApiController]
    public class AuthController : ControllerBase
    {
        //
        // Bloco: Injeção de Dependências
        // Serviços essenciais para a gestão de utilizadores, login e criação de tokens.
        //
        private readonly UserManager<SIGHRUser> _userManager;
        private readonly SignInManager<SIGHRUser> _signInManager;
        private readonly TokenService _tokenService;

        public AuthController(
            UserManager<SIGHRUser> userManager,
            SignInManager<SIGHRUser> signInManager,
            TokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Endpoint para autenticar um utilizador e retornar um token JWT se as credenciais forem válidas.
        /// Rota: POST api/Auth/login
        /// </summary>
        /// <param name="model">Objeto com UserName e Password.</param>
        /// <returns>Um objeto com o token JWT e uma mensagem de sucesso, ou um erro 401/400.</returns>
        [HttpPost("login")]
        [AllowAnonymous] // Permite que qualquer pessoa (não autenticada) chame este endpoint.
        public async Task<IActionResult> Login([FromBody] LoginApiViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Procura o utilizador pelo nome fornecido.
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                // Para não dar pistas a potenciais atacantes, retornamos a mesma mensagem de erro genérica.
                return Unauthorized(new { message = "Nome de utilizador ou palavra-passe inválidos." });
            }

            // Apenas verifica a palavra-passe, sem criar um cookie de sessão.
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Nome de utilizador ou palavra-passe inválidos." });
            }

            // Se as credenciais estiverem corretas, gera o token JWT.
            var tokenString = await _tokenService.GenerateToken(user);

            // Retorna uma resposta de sucesso (200 OK) com o token e alguns dados úteis do utilizador.
            return Ok(new
            {
                message = "Login bem-sucedido.",
                token = tokenString,
                user = new
                {
                    username = user.UserName,
                    email = user.Email,
                    nomeCompleto = user.NomeCompleto,
                    tipo = user.Tipo // A propriedade 'Tipo' que utiliza para a lógica de negócio.
                }
            });
        }
    }

    /// <summary>
    /// Modelo de dados (ViewModel) para receber as credenciais de login da API.
    /// Define a estrutura e as regras de validação para o corpo do pedido de login.
    /// </summary>
    public class LoginApiViewModel
    {
        [Required(ErrorMessage = "O Nome de Utilizador é obrigatório.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Palavra-passe é obrigatória.")]
        public string Password { get; set; } = string.Empty;
    }
}