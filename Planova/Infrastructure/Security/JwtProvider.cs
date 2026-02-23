using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Planova.Application.Common.Interfaces;

namespace Planova.Infrastructure.Security
{
    public sealed class JwtProvider : IJwtProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtProvider> _logger;

		public JwtProvider(IConfiguration configuration, ILogger<JwtProvider> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public string Generate(Guid userId, string email, string role)
        {
            _logger.LogInformation("Generating JWT for user ID: {UserId}, email: {Email}, role: {Role}", userId, email, role);

			var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expires = DateTime.UtcNow.AddHours(2);

            var claims = new[]
            {
				new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
	            new Claim(ClaimTypes.Email, email),
	            new Claim(ClaimTypes.Role, role)
			};

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            _logger.LogInformation("JWT generated successfully for user ID: {UserId}", userId);

			return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}