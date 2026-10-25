using Microsoft.IdentityModel.Tokens;
using OnlineCourseManagement.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OnlineCourseManagement.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            var secret = _config["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Jwt:Key");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}