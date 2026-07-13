using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Application.DTOs.Auth;

namespace OficinaMecanica.Infrastructure.Security;

/// <summary>Autenticação simples (usuário admin de configuração) + emissão de JWT.</summary>
public class JwtAutenticador : IAutenticador
{
    private readonly IConfiguration _config;

    public JwtAutenticador(IConfiguration config)
    {
        _config = config;
    }

    public TokenDto? Autenticar(LoginDto dto)
    {
        var expectedUsername = _config["Jwt:AdminUsername"];
        var expectedPassword = _config["Jwt:AdminPassword"];

        if (dto.Username != expectedUsername || dto.Password != expectedPassword)
            return null;

        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurado.");
        var issuer = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;
        var expiresInMinutes = int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60");

        var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, dto.Username),
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return new TokenDto(handler.WriteToken(token), expiresAt);
    }
}
