using Microsoft.EntityFrameworkCore;
using OficinaMecanicaBackend.Data;
using OficinaMecanicaBackend.DTOs.Servicos;
using OficinaMecanicaBackend.Models;

namespace OficinaMecanicaBackend.Services;

public class ServicoService
{
    private readonly AppDbContext _db;

    public ServicoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<IEnumerable<ServicoDto>>> GetAllAsync()
    {
        var servicos = await _db.Servicos.AsNoTracking().ToListAsync();
        return ServiceResult<IEnumerable<ServicoDto>>.Ok(servicos.Select(ToDto));
    }

    public async Task<ServiceResult<ServicoDto>> GetByIdAsync(int id)
    {
        var servico = await _db.Servicos.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (servico is null)
            return ServiceResult<ServicoDto>.NotFound($"Serviço {id} não encontrado.");
        return ServiceResult<ServicoDto>.Ok(ToDto(servico));
    }

    public async Task<ServiceResult<ServicoDto>> CreateAsync(CreateServicoDto dto)
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

        _db.Servicos.Add(servico);
        await _db.SaveChangesAsync();
        return ServiceResult<ServicoDto>.Created(ToDto(servico));
    }

    public async Task<ServiceResult<ServicoDto>> UpdateAsync(int id, UpdateServicoDto dto)
    {
        var servico = await _db.Servicos.FindAsync(id);
        if (servico is null)
            return ServiceResult<ServicoDto>.NotFound($"Serviço {id} não encontrado.");

        servico.Nome = dto.Nome;
        servico.Descricao = dto.Descricao;
        servico.PrecoBase = dto.PrecoBase;
        servico.TempoEstimadoHoras = dto.TempoEstimadoHoras;
        servico.Ativo = dto.Ativo;
        servico.DataAtualizacao = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult<ServicoDto>.Ok(ToDto(servico));
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var servico = await _db.Servicos.FindAsync(id);
        if (servico is null)
            return ServiceResult<object>.NotFound($"Serviço {id} não encontrado.");

        _db.Servicos.Remove(servico);
        await _db.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static ServicoDto ToDto(Servico s) => new(
        s.Id, s.Nome, s.Descricao, s.PrecoBase, s.TempoEstimadoHoras,
        s.Ativo, s.DataCriacao, s.DataAtualizacao
    );
}
