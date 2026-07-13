using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.UseCases;

namespace OficinaMecanica.API.Controllers;

[ApiController]
[Route("api/pecas")]
[Authorize]
public class PecasController : ControllerBase
{
    private readonly PecaUseCases _useCases;

    public PecasController(PecaUseCases useCases)
    {
        _useCases = useCases;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _useCases.ListarAsync();
        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _useCases.ObterAsync(id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpGet("estoque-baixo")]
    public async Task<IActionResult> GetEstoqueBaixo()
    {
        var result = await _useCases.ListarEstoqueBaixoAsync();
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePecaDto dto)
    {
        var result = await _useCases.CriarAsync(dto);
        return result.StatusCode switch
        {
            201 => CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data),
            409 => Conflict(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePecaDto dto)
    {
        var result = await _useCases.AtualizarAsync(id, dto);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            409 => Conflict(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _useCases.ExcluirAsync(id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => NoContent()
        };
    }
}
