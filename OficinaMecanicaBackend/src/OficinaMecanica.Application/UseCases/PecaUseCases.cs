using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Application.Common;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases;

public class PecaUseCases
{
    private readonly IPecaRepository _pecas;
    private readonly IUnitOfWork _uow;

    public PecaUseCases(IPecaRepository pecas, IUnitOfWork uow)
    {
        _pecas = pecas;
        _uow = uow;
    }

    public async Task<ServiceResult<IEnumerable<PecaDto>>> ListarAsync()
    {
        var pecas = await _pecas.ListarAsync();
        return ServiceResult<IEnumerable<PecaDto>>.Ok(pecas.Select(ToDto));
    }

    public async Task<ServiceResult<PecaDto>> ObterAsync(int id)
    {
        var peca = await _pecas.ObterAsync(id);
        if (peca is null)
            return ServiceResult<PecaDto>.NotFound($"Peça {id} não encontrada.");
        return ServiceResult<PecaDto>.Ok(ToDto(peca));
    }

    public async Task<ServiceResult<IEnumerable<PecaDto>>> ListarEstoqueBaixoAsync()
    {
        var pecas = await _pecas.ListarEstoqueBaixoAsync();
        return ServiceResult<IEnumerable<PecaDto>>.Ok(pecas.Select(ToDto));
    }

    public async Task<ServiceResult<PecaDto>> CriarAsync(CreatePecaDto dto)
    {
        var codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo.Trim();

        if (codigo is not null && await _pecas.ExistePorCodigoAsync(codigo))
            return ServiceResult<PecaDto>.Conflict("Código de peça já cadastrado.");

        var peca = new Peca
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Codigo = codigo,
            PrecoUnitario = dto.PrecoUnitario,
            QuantidadeEstoque = dto.QuantidadeEstoque,
            QuantidadeMinima = dto.QuantidadeMinima,
            Ativo = dto.Ativo,
            DataCriacao = DateTime.UtcNow
        };

        await _pecas.AdicionarAsync(peca);
        await _uow.SalvarAsync();
        return ServiceResult<PecaDto>.Created(ToDto(peca));
    }

    public async Task<ServiceResult<PecaDto>> AtualizarAsync(int id, UpdatePecaDto dto)
    {
        var peca = await _pecas.ObterAsync(id, tracking: true);
        if (peca is null)
            return ServiceResult<PecaDto>.NotFound($"Peça {id} não encontrada.");

        var codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo.Trim();

        if (codigo is not null && await _pecas.ExistePorCodigoAsync(codigo, ignorarId: id))
            return ServiceResult<PecaDto>.Conflict("Código de peça já cadastrado.");

        peca.Nome = dto.Nome;
        peca.Descricao = dto.Descricao;
        peca.Codigo = codigo;
        peca.PrecoUnitario = dto.PrecoUnitario;
        peca.QuantidadeEstoque = dto.QuantidadeEstoque;
        peca.QuantidadeMinima = dto.QuantidadeMinima;
        peca.Ativo = dto.Ativo;
        peca.DataAtualizacao = DateTime.UtcNow;

        await _uow.SalvarAsync();
        return ServiceResult<PecaDto>.Ok(ToDto(peca));
    }

    public async Task<ServiceResult<object>> ExcluirAsync(int id)
    {
        var peca = await _pecas.ObterAsync(id, tracking: true);
        if (peca is null)
            return ServiceResult<object>.NotFound($"Peça {id} não encontrada.");

        _pecas.Remover(peca);
        await _uow.SalvarAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static PecaDto ToDto(Peca p) => new(
        p.Id, p.Nome, p.Descricao, p.Codigo, p.PrecoUnitario,
        p.QuantidadeEstoque, p.QuantidadeMinima, p.Ativo,
        p.EstoqueBaixo, p.DataCriacao, p.DataAtualizacao
    );
}
