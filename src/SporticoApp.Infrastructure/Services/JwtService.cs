using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Utilities;
using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        public JwtService(IConfiguration config)
        {
            _config = config;
        }
        public TokenResult GenerateAccessToken(User user)
        {
            var secretKey = _config["JWT:SecretKey"];
            var issuer = _config["JWT:Issuer"];
            var audience = _config["JWT:Audience"];

            if (string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "JWT configuration is missing required values.");
            }

            var claims = new List<Claim> 
            { 
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            };
            foreach (var role in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Role.Name));
            }
            
            var expiresInMinutes = _config.GetValue<int>(
                "JWT:AccessTokenExpirationMinutes");
            if (expiresInMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "JWT:AccessTokenExpirationMinutes must be greater than zero.");
            }
            var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);
            
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );
            //return new JwtSecurityTokenHandler().WriteToken(token);
            return new TokenResult()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt
            };
        }
    }
}
