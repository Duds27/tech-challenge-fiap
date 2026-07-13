using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Application.DTOs.Auth;

namespace OficinaMecanica.Application.UseCases;

public class AutenticarUseCase
{
    private readonly IAutenticador _autenticador;

    public AutenticarUseCase(IAutenticador autenticador)
    {
        _autenticador = autenticador;
    }

    public TokenDto? Login(LoginDto dto) => _autenticador.Autenticar(dto);
}
