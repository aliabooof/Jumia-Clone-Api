using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _setting;

        public JwtService(IOptions<JwtSettings> options)
        {
            _setting = options.Value;
        }

        public async Task<string> GenerateJwtTokenAsync (AppUser user,string role, int userTypeId)
        {
            var claims = new List<Claim>();
            
                if (!string.IsNullOrEmpty(role))
                claims.Add(new Claim(ClaimTypes.Role, role));

            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            if (!string.IsNullOrEmpty(user.Id))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));

            if (!string.IsNullOrEmpty(user.UserName))
                claims.Add(new Claim(ClaimTypes.Name, user.UserName));
        
            if (userTypeId != 0)
            {
                claims.Add(new Claim("userTypeId",userTypeId.ToString()));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_setting.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _setting.Issuer,
                audience: _setting.Audience,
                claims: claims,
                expires: DateTime.Now.AddDays(_setting.DurationInDays),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
