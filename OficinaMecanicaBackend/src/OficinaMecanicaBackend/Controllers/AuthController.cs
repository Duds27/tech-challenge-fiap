using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaBackend.DTOs.Auth;
using OficinaMecanicaBackend.Services;

namespace OficinaMecanicaBackend.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AuthService _service;

    public AuthController(AuthService service)
    {
        _service = service;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        var token = _service.Login(dto);
        if (token is null)
            return Unauthorized(new { error = "Usuário ou senha inválidos." });
        return Ok(token);
    }
}
