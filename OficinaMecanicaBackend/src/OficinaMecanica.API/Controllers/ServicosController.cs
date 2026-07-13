using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.DTOs.Servicos;
using OficinaMecanica.Application.UseCases;

namespace OficinaMecanica.API.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize]
public class ServicosController : ControllerBase
{
    private readonly ServicoUseCases _useCases;

    public ServicosController(ServicoUseCases useCases)
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServicoDto dto)
    {
        var result = await _useCases.CriarAsync(dto);
        return result.StatusCode switch
        {
            201 => CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data),
            _   => Ok(result.Data)
        };
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServicoDto dto)
    {
        var result = await _useCases.AtualizarAsync(id, dto);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
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
