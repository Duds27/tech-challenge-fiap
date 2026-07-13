using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.DTOs.OrdensServico;
using OficinaMecanica.Application.UseCases;

namespace OficinaMecanica.API.Controllers;

[ApiController]
[Route("api/ordens-servico")]
[Authorize]
public class OrdensServicoController : ControllerBase
{
    private readonly OrdemServicoUseCases _useCases;

    public OrdensServicoController(OrdemServicoUseCases useCases)
    {
        _useCases = useCases;
    }

    /// <summary>
    /// Lista as ordens de serviço ativas, ordenadas por prioridade de status
    /// (Em Execução → Aguardando Aprovação → Em Diagnóstico → Recebida) e, dentro
    /// do mesmo status, das mais antigas para as mais recentes. OS finalizadas e
    /// entregues são omitidas, exceto quando <paramref name="incluirConcluidas"/> = true.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirConcluidas = false)
    {
        var result = await _useCases.ListarAsync(incluirConcluidas);
        return Ok(result.Data);
    }

    /// <summary>
    /// Retorna o tempo médio de execução das ordens de serviço, calculado do
    /// início da execução até a finalização. Considera apenas ordens finalizadas.
    /// </summary>
    [HttpGet("tempo-medio-execucao")]
    [ProducesResponseType(typeof(TempoMedioExecucaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTempoMedioExecucao()
    {
        var result = await _useCases.CalcularTempoMedioExecucaoAsync();
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

    [HttpGet("{id:int}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(int id)
    {
        var result = await _useCases.ObterStatusPublicoAsync(id);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrdemServicoDto dto)
    {
        var result = await _useCases.CriarAsync(dto);
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
        var result = await _useCases.AvancarStatusAsync(id, dto);
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
        var result = await _useCases.AprovarOrcamentoAsync(id, dto);
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
        var result = await _useCases.AdicionarItemAsync(id, dto);
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
        var result = await _useCases.RemoverItemAsync(id, itemId);
        return result.StatusCode switch
        {
            404 => NotFound(new { error = result.Error }),
            _   => Ok(result.Data)
        };
    }
}
