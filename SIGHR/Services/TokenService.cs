// Services/TokenService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using SIGHR.Areas.Identity.Data;

namespace SIGHR.Services
{
    /// <summary>
    /// Serviço responsável por gerar Tokens de Acesso JWT (JSON Web Tokens).
    /// Estes tokens são usados para autenticar utilizadores em pedidos de API.
    /// </summary>
    public class TokenService
    {
        //
        // Bloco: Injeção de Dependências
        //
        private readonly IConfiguration _config;
        private readonly UserManager<SIGHRUser> _userManager;

        public TokenService(IConfiguration config, UserManager<SIGHRUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        /// <summary>
        /// Gera um token JWT para um utilizador específico.
        /// </summary>
        /// <param name="user">O objeto do utilizador para quem o token será gerado.</param>
        /// <returns>Uma string que representa o token JWT.</returns>
        public async Task<string> GenerateToken(SIGHRUser user)
        {
            // Bloco: Carregar Configurações do Token
            // Obtém as configurações do token (chave secreta, emissor, audiência) do ficheiro appsettings.json.
            var jwtSettings = _config.GetSection("Jwt");
            var jwtKeyString = jwtSettings["Key"] ?? throw new InvalidOperationException("A Chave JWT (Jwt:Key) não está configurada em appsettings.json.");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKeyString));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Bloco: Criar as "Claims" (Informações de Identidade)
            // As claims são peças de informação sobre o utilizador que serão incluídas no token.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),           // 'Subject' (o ID único do utilizador)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // 'JWT ID' (um ID único para este token específico)
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!), // O nome de utilizador
            };

            // Adiciona claims opcionais, se existirem.
            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            }
            if (!string.IsNullOrEmpty(user.NomeCompleto))
            {
                claims.Add(new Claim("FullName", user.NomeCompleto)); // Claim personalizada
            }

            // Adiciona as funções (Roles) do utilizador como claims.
            // Isto é crucial para a autorização baseada em funções na API.
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            if (!string.IsNullOrEmpty(user.Tipo))
            {
                claims.Add(new Claim("userType", user.Tipo)); // Claim personalizada para o 'Tipo'
            }

            // Bloco: Montar e Gerar o Token
            // Cria o objeto do token com todas as informações e configurações.
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpireHours"] ?? "1")), // Define a validade do token
                signingCredentials: credentials);

            // Serializa o token para o seu formato final de string compacta.
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}