using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Support_Ticket_Management_API.Models;

namespace Support_Ticket_Management_API.Services
{
    public class TokenService
    {
        private readonly string _secret;
        private readonly string _issuer;

        public TokenService(IConfiguration config)
        {
            _secret = config["Jwt:Secret"] ?? "super_secret_dev_key_change";
            _issuer = config["Jwt:Issuer"] ?? "SupportTicketApi";
        }

        public string GenerateToken(User user, Role role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role.Name.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
