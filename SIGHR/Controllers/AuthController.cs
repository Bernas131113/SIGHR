// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIGHR.Areas.Identity.Data;
using SIGHR.Models.ViewModels; // Onde LoginApiViewModel está definido
using SIGHR.Services;  // Onde TokenService está definido
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SIGHR.Controllers
{
    [Route("api/[controller]")] // Define a rota base como /api/Auth
    [ApiController]
    public class AuthController : ControllerBase
    {
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
        /// Autentica um utilizador e retorna um token JWT se as credenciais forem válidas.
        /// </summary>
        /// <param name="model">Objeto com UserName e Password.</param>
        /// <returns>Um objeto com o token JWT e uma mensagem de sucesso, ou um erro 401/400.</returns>
        [HttpPost("login")]
        [AllowAnonymous] // Permite que qualquer um (não autenticado) chame este endpoint
        public async Task<IActionResult> Login([FromBody] LoginApiViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Busca o usuário pelo nome de usuário fornecido
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                // Para não dar pistas a atacantes, retornamos a mesma mensagem de erro genérica
                return Unauthorized(new { message = "Nome de utilizador ou senha inválidos." });
            }

            // Verifica a senha (NÃO loga o usuário com um cookie, apenas verifica)
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Nome de utilizador ou senha inválidos." });
            }

            // Se as credenciais estiverem corretas, gera o token JWT
            var tokenString = await _tokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Login bem-sucedido.",
                token = tokenString,
                user = new // Retornar alguns dados úteis do usuário é comum
                {
                    username = user.UserName,
                    email = user.Email,
                    nomeCompleto = user.NomeCompleto,
                    tipo = user.Tipo // A propriedade 'Tipo' que você usa
                }
            });
        }
    }

    // Você pode colocar esta classe no final do arquivo AuthController.cs ou
    // em Models/ViewModels/LoginApiViewModel.cs
    public class LoginApiViewModel
    {
        [Required(ErrorMessage = "O Nome de Utilizador é obrigatório.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        public string Password { get; set; } = string.Empty;
    }
}