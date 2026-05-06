using System.Data;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaBackend.Data;
using OficinaMecanicaBackend.DTOs.Pecas;
using OficinaMecanicaBackend.Models;

namespace OficinaMecanicaBackend.Services;

public class PecaService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PecaService> _logger;

    public PecaService(AppDbContext db, ILogger<PecaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<PecaDto>>> GetAllAsync()
    {
        var pecas = await _db.Pecas.AsNoTracking().ToListAsync();
        return ServiceResult<IEnumerable<PecaDto>>.Ok(pecas.Select(ToDto));
    }

    public async Task<ServiceResult<PecaDto>> GetByIdAsync(int id)
    {
        var peca = await _db.Pecas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (peca is null)
            return ServiceResult<PecaDto>.NotFound($"Peça {id} não encontrada.");
        return ServiceResult<PecaDto>.Ok(ToDto(peca));
    }

    public async Task<ServiceResult<IEnumerable<PecaDto>>> GetEstoqueBaixoAsync()
    {
        var pecas = await _db.Pecas.AsNoTracking()
            .Where(p => p.QuantidadeEstoque < p.QuantidadeMinima)
            .ToListAsync();
        return ServiceResult<IEnumerable<PecaDto>>.Ok(pecas.Select(ToDto));
    }

    public async Task<ServiceResult<PecaDto>> CreateAsync(CreatePecaDto dto)
    {
        var codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo.Trim();

        if (codigo is not null)
        {
            var codigoExiste = await _db.Pecas.AnyAsync(p => p.Codigo == codigo);
            if (codigoExiste)
                return ServiceResult<PecaDto>.Conflict("Código de peça já cadastrado.");
        }

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

        _db.Pecas.Add(peca);
        await _db.SaveChangesAsync();
        return ServiceResult<PecaDto>.Created(ToDto(peca));
    }

    public async Task<ServiceResult<PecaDto>> UpdateAsync(int id, UpdatePecaDto dto)
    {
        var peca = await _db.Pecas.FindAsync(id);
        if (peca is null)
            return ServiceResult<PecaDto>.NotFound($"Peça {id} não encontrada.");

        var codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? null : dto.Codigo.Trim();

        if (codigo is not null)
        {
            var codigoExiste = await _db.Pecas.AnyAsync(p => p.Codigo == codigo && p.Id != id);
            if (codigoExiste)
                return ServiceResult<PecaDto>.Conflict("Código de peça já cadastrado.");
        }

        peca.Nome = dto.Nome;
        peca.Descricao = dto.Descricao;
        peca.Codigo = codigo;
        peca.PrecoUnitario = dto.PrecoUnitario;
        peca.QuantidadeEstoque = dto.QuantidadeEstoque;
        peca.QuantidadeMinima = dto.QuantidadeMinima;
        peca.Ativo = dto.Ativo;
        peca.DataAtualizacao = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult<PecaDto>.Ok(ToDto(peca));
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var peca = await _db.Pecas.FindAsync(id);
        if (peca is null)
            return ServiceResult<object>.NotFound($"Peça {id} não encontrada.");

        _db.Pecas.Remove(peca);
        await _db.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { });
    }

    public async Task<ServiceResult<object>> DeductStockAsync(int pecaId, decimal qty)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        try
        {
            var peca = await _db.Pecas.FindAsync(pecaId);
            if (peca is null)
                return ServiceResult<object>.NotFound($"Peça {pecaId} não encontrada.");

            if (peca.QuantidadeEstoque < (int)qty)
                return ServiceResult<object>.BadRequest(
                    $"Estoque insuficiente. Disponível: {peca.QuantidadeEstoque}, solicitado: {qty}.");

            peca.QuantidadeEstoque -= (int)qty;
            peca.DataAtualizacao = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            if (peca.QuantidadeEstoque < peca.QuantidadeMinima)
                _logger.LogWarning("Peça {PecaId} ({Nome}) abaixo do estoque mínimo: {Atual}/{Minimo}",
                    peca.Id, peca.Nome, peca.QuantidadeEstoque, peca.QuantidadeMinima);

            return ServiceResult<object>.Ok(new { });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult<object>> RestoreStockAsync(int pecaId, decimal qty)
    {
        var peca = await _db.Pecas.FindAsync(pecaId);
        if (peca is null)
            return ServiceResult<object>.NotFound($"Peça {pecaId} não encontrada.");

        peca.QuantidadeEstoque += (int)qty;
        peca.DataAtualizacao = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static PecaDto ToDto(Peca p) => new(
        p.Id, p.Nome, p.Descricao, p.Codigo, p.PrecoUnitario,
        p.QuantidadeEstoque, p.QuantidadeMinima, p.Ativo,
        p.QuantidadeEstoque < p.QuantidadeMinima,
        p.DataCriacao, p.DataAtualizacao
    );
}
