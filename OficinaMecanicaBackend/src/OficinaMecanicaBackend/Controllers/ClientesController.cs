using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaBackend.DTOs.Clientes;
using OficinaMecanicaBackend.Services;

namespace OficinaMecanicaBackend.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;

    public ClientesController(ClienteService service)
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
    public async Task<IActionResult> Create([FromBody] CreateClienteDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.StatusCode switch
        {
            201 => CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data),
            400 => BadRequest(new { error = result.Error }),
            409 => Conflict(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClienteDto dto)
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
            409 => Conflict(new { error = result.Error }),
            _   => NoContent()
        };
    }
}
