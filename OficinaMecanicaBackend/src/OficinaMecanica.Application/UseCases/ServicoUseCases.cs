using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Application.Common;
using OficinaMecanica.Application.DTOs.Servicos;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases;

public class ServicoUseCases
{
    private readonly IServicoRepository _servicos;
    private readonly IUnitOfWork _uow;

    public ServicoUseCases(IServicoRepository servicos, IUnitOfWork uow)
    {
        _servicos = servicos;
        _uow = uow;
    }

    public async Task<ServiceResult<IEnumerable<ServicoDto>>> ListarAsync()
    {
        var servicos = await _servicos.ListarAsync();
        return ServiceResult<IEnumerable<ServicoDto>>.Ok(servicos.Select(ToDto));
    }

    public async Task<ServiceResult<ServicoDto>> ObterAsync(int id)
    {
        var servico = await _servicos.ObterAsync(id);
        if (servico is null)
            return ServiceResult<ServicoDto>.NotFound($"Serviço {id} não encontrado.");
        return ServiceResult<ServicoDto>.Ok(ToDto(servico));
    }

    public async Task<ServiceResult<ServicoDto>> CriarAsync(CreateServicoDto dto)
    {
        var servico = new Servico
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            PrecoBase = dto.PrecoBase,
            TempoEstimadoHoras = dto.TempoEstimadoHoras,
            Ativo = dto.Ativo,
            DataCriacao = DateTime.UtcNow
        };

        await _servicos.AdicionarAsync(servico);
        await _uow.SalvarAsync();
        return ServiceResult<ServicoDto>.Created(ToDto(servico));
    }

    public async Task<ServiceResult<ServicoDto>> AtualizarAsync(int id, UpdateServicoDto dto)
    {
        var servico = await _servicos.ObterAsync(id, tracking: true);
        if (servico is null)
            return ServiceResult<ServicoDto>.NotFound($"Serviço {id} não encontrado.");

        servico.Nome = dto.Nome;
        servico.Descricao = dto.Descricao;
        servico.PrecoBase = dto.PrecoBase;
        servico.TempoEstimadoHoras = dto.TempoEstimadoHoras;
        servico.Ativo = dto.Ativo;
        servico.DataAtualizacao = DateTime.UtcNow;

        await _uow.SalvarAsync();
        return ServiceResult<ServicoDto>.Ok(ToDto(servico));
    }

    public async Task<ServiceResult<object>> ExcluirAsync(int id)
    {
        var servico = await _servicos.ObterAsync(id, tracking: true);
        if (servico is null)
            return ServiceResult<object>.NotFound($"Serviço {id} não encontrado.");

        _servicos.Remover(servico);
        await _uow.SalvarAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static ServicoDto ToDto(Servico s) => new(
        s.Id, s.Nome, s.Descricao, s.PrecoBase, s.TempoEstimadoHoras,
        s.Ativo, s.DataCriacao, s.DataAtualizacao
    );
}
