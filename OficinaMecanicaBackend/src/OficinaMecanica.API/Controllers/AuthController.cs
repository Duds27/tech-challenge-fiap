using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.DTOs.Auth;
using OficinaMecanica.Application.UseCases;

namespace OficinaMecanica.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AutenticarUseCase _useCase;

    public AuthController(AutenticarUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        var token = _useCase.Login(dto);
        if (token is null)
            return Unauthorized(new { error = "Usuário ou senha inválidos." });
        return Ok(token);
    }
}
