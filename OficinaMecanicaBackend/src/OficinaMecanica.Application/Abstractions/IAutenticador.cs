using OficinaMecanica.Application.DTOs.Auth;

namespace OficinaMecanica.Application.Abstractions;

/// <summary>Porta de saída para autenticação e emissão de token JWT.</summary>
public interface IAutenticador
{
    TokenDto? Autenticar(LoginDto dto);
}
