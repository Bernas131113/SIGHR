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

namespace SIGHR.Services // Certifique-se que o namespace corresponde à localização da pasta
{
    public class TokenService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<SIGHRUser> _userManager;

        public TokenService(IConfiguration config, UserManager<SIGHRUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public async Task<string> GenerateToken(SIGHRUser user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var jwtKeyString = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key não está configurada.");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKeyString));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Criar a lista de claims para o token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),           // Subject (ID único do usuário)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID único para o token
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!), // Nome de usuário
            };

            // Adicionar claims de Email, se existir
            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            }
            // Adicionar claim customizada para Nome Completo, se existir
            if (!string.IsNullOrEmpty(user.NomeCompleto))
            {
                claims.Add(new Claim("FullName", user.NomeCompleto));
            }

            // Adicionar todos os roles do usuário como claims de Role
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            // Adicionar também o Tipo do usuário como uma claim, pode ser útil
            if (!string.IsNullOrEmpty(user.Tipo))
            {
                claims.Add(new Claim("userType", user.Tipo)); // claim customizada 'userType'
            }

            // Montar o token
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpireHours"] ?? "1")),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}