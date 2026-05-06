using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaBackend.DTOs.Servicos;
using OficinaMecanicaBackend.Services;

namespace OficinaMecanicaBackend.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize]
public class ServicosController : ControllerBase
{
    private readonly ServicoService _service;

    public ServicosController(ServicoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServicoDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.StatusCode switch
        {
            201 => CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data),
            _   => Ok(result.Data)
        };
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServicoDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => NoContent()
        };
    }
}
