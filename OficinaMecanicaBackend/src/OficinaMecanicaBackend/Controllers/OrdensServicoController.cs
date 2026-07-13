using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaBackend.DTOs.OrdensServico;
using OficinaMecanicaBackend.Services;

namespace OficinaMecanicaBackend.Controllers;

[ApiController]
[Route("api/ordens-servico")]
[Authorize]
public class OrdensServicoController : ControllerBase
{
    private readonly OrdemServicoService _service;

    public OrdensServicoController(OrdemServicoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result.Data);
    }

    /// <summary>
    /// Retorna o tempo médio de execução das ordens de serviço, calculado do
    /// momento de criação até a finalização. Considera apenas ordens finalizadas.
    /// </summary>
    [HttpGet("tempo-medio-execucao")]
    [ProducesResponseType(typeof(TempoMedioExecucaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTempoMedioExecucao()
    {
        var result = await _service.GetTempoMedioExecucaoAsync();
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

    [HttpGet("{id:int}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(int id)
    {
        var result = await _service.GetStatusPublicoAsync(id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrdemServicoDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.StatusCode switch
        {
            201 => CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data),
            400 => BadRequest(new { error = result.Error }),
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AdvanceStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var result = await _service.AdvanceStatusAsync(id, dto);
        return result.StatusCode switch
        {
            400 => BadRequest(new { error = result.Error }),
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPut("{id:int}/aprovar-orcamento")]
    [AllowAnonymous]
    public async Task<IActionResult> AprovarOrcamento(int id, [FromBody] AprovarOrcamentoDto dto)
    {
        var result = await _service.AprovarOrcamentoAsync(id, dto);
        return result.StatusCode switch
        {
            400 => BadRequest(new { error = result.Error }),
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPost("{id:int}/itens")]
    public async Task<IActionResult> AddItem(int id, [FromBody] AddItemDto dto)
    {
        var result = await _service.AddItemAsync(id, dto);
        return result.StatusCode switch
        {
            201 => CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data),
            400 => BadRequest(new { error = result.Error }),
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpDelete("{id:int}/itens/{itemId:int}")]
    public async Task<IActionResult> DeleteItem(int id, int itemId)
    {
        var result = await _service.DeleteItemAsync(id, itemId);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }
}
