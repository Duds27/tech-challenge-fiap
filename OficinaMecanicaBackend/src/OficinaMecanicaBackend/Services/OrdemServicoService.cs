using Microsoft.EntityFrameworkCore;
using OficinaMecanicaBackend.Data;
using OficinaMecanicaBackend.DTOs.OrdensServico;
using OficinaMecanicaBackend.Models;
using OficinaMecanicaBackend.Models.Enums;

namespace OficinaMecanicaBackend.Services;

public class OrdemServicoService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrdemServicoService> _logger;

    public OrdemServicoService(AppDbContext db, ILogger<OrdemServicoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<OrdemServicoDto>>> GetAllAsync()
    {
        var ordens = await _db.OrdensServico
            .AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Veiculo)
            .Include(o => o.Itens).ThenInclude(i => i.Servico)
            .Include(o => o.Itens).ThenInclude(i => i.Peca)
            .ToListAsync();
        return ServiceResult<IEnumerable<OrdemServicoDto>>.Ok(ordens.Select(ToDto));
    }

    public async Task<ServiceResult<OrdemServicoDto>> GetByIdAsync(int id)
    {
        var ordem = await FindWithIncludes(id);
        if (ordem is null)
            return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");
        return ServiceResult<OrdemServicoDto>.Ok(ToDto(ordem));
    }

    public async Task<ServiceResult<StatusPublicoDto>> GetStatusPublicoAsync(int id)
    {
        var ordem = await _db.OrdensServico.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
        if (ordem is null)
            return ServiceResult<StatusPublicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");

        return ServiceResult<StatusPublicoDto>.Ok(new StatusPublicoDto(
            ordem.NumeroOS,
            ordem.Status,
            ordem.Status.ToString(),
            ordem.DataPrevisaoTermino
        ));
    }

    public async Task<ServiceResult<TempoMedioExecucaoDto>> GetTempoMedioExecucaoAsync()
    {
        var periodos = await _db.OrdensServico
            .AsNoTracking()
            .Where(o => o.DataFinalizacao != null)
            .Select(o => new { o.DataCriacao, DataFinalizacao = o.DataFinalizacao!.Value })
            .ToListAsync();

        if (periodos.Count == 0)
            return ServiceResult<TempoMedioExecucaoDto>.Ok(
                new TempoMedioExecucaoDto(0, 0, 0, 0, "00:00:00"));

        var mediaSegundos = periodos.Average(p => (p.DataFinalizacao - p.DataCriacao).TotalSeconds);
        var media = TimeSpan.FromSeconds(mediaSegundos);

        return ServiceResult<TempoMedioExecucaoDto>.Ok(new TempoMedioExecucaoDto(
            periodos.Count,
            Math.Round(mediaSegundos, 2),
            Math.Round(media.TotalMinutes, 2),
            Math.Round(media.TotalHours, 2),
            $"{(int)media.TotalHours:D2}:{media.Minutes:D2}:{media.Seconds:D2}"
        ));
    }

    public async Task<ServiceResult<OrdemServicoDto>> CreateAsync(CreateOrdemServicoDto dto)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId);
        if (!clienteExiste)
            return ServiceResult<OrdemServicoDto>.NotFound($"Cliente {dto.ClienteId} não encontrado.");

        var veiculoExiste = await _db.Veiculos.AnyAsync(v => v.Id == dto.VeiculoId && v.ClienteId == dto.ClienteId);
        if (!veiculoExiste)
            return ServiceResult<OrdemServicoDto>.NotFound($"Veículo {dto.VeiculoId} não encontrado ou não pertence ao cliente.");

        var numeroOS = await GerarNumeroOSAsync();

        var ordem = new OrdemServico
        {
            NumeroOS = numeroOS,
            ClienteId = dto.ClienteId,
            VeiculoId = dto.VeiculoId,
            Status = StatusOrdemServico.Recebida,
            Descricao = dto.Descricao,
            ValorTotal = 0,
            OrcamentoAprovado = false,
            DataPrevisaoTermino = dto.DataPrevisaoTermino,
            DataCriacao = DateTime.UtcNow
        };

        _db.OrdensServico.Add(ordem);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Retry once on NumeroOS unique constraint collision (race condition)
            ordem.NumeroOS = await GerarNumeroOSAsync();
            await _db.SaveChangesAsync();
        }

        var ordemComIncludes = await FindWithIncludes(ordem.Id);
        return ServiceResult<OrdemServicoDto>.Created(ToDto(ordemComIncludes!));
    }

    public async Task<ServiceResult<OrdemServicoDto>> AdvanceStatusAsync(int id, UpdateStatusDto dto)
    {
        var ordem = await FindWithIncludes(id);
        if (ordem is null)
            return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");

        var (valid, error) = ValidateTransition(ordem, dto.NovoStatus);
        if (!valid)
            return ServiceResult<OrdemServicoDto>.BadRequest(error!);

        ordem.Status = dto.NovoStatus;

        if (dto.NovoStatus == StatusOrdemServico.Finalizada)
            ordem.DataFinalizacao = DateTime.UtcNow;
        else if (dto.NovoStatus == StatusOrdemServico.Entregue)
            ordem.DataEntrega = DateTime.UtcNow;

        ordem.DataAtualizacao = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ServiceResult<OrdemServicoDto>.Ok(ToDto(ordem));
    }

    public async Task<ServiceResult<OrdemServicoDto>> AprovarOrcamentoAsync(int id, AprovarOrcamentoDto dto)
    {
        var ordem = await FindWithIncludes(id);
        if (ordem is null)
            return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");

        if (ordem.Status != StatusOrdemServico.AguardandoAprovacao)
            return ServiceResult<OrdemServicoDto>.BadRequest(
                "Aprovação de orçamento só é possível quando o status é 'AguardandoAprovacao'.");

        ordem.OrcamentoAprovado = dto.Aprovado;
        ordem.DataAprovacaoOrcamento = DateTime.UtcNow;
        ordem.DataAtualizacao = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult<OrdemServicoDto>.Ok(ToDto(ordem));
    }

    public async Task<ServiceResult<OrdemServicoDto>> AddItemAsync(int ordemServicoId, AddItemDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var ordem = await FindWithIncludes(ordemServicoId);
            if (ordem is null)
                return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {ordemServicoId} não encontrada.");

            if (ordem.Status is StatusOrdemServico.Finalizada or StatusOrdemServico.Entregue)
                return ServiceResult<OrdemServicoDto>.BadRequest("Não é possível adicionar itens a uma OS finalizada ou entregue.");

            decimal precoUnitario;

            if (dto.Tipo == TipoItemOrdemServico.Servico)
            {
                var servico = await _db.Servicos.FindAsync(dto.ServicoId!.Value);
                if (servico is null)
                    return ServiceResult<OrdemServicoDto>.NotFound($"Serviço {dto.ServicoId} não encontrado.");
                precoUnitario = servico.PrecoBase;
            }
            else
            {
                var peca = await _db.Pecas.FindAsync(dto.PecaId!.Value);
                if (peca is null)
                    return ServiceResult<OrdemServicoDto>.NotFound($"Peça {dto.PecaId} não encontrada.");

                if (peca.QuantidadeEstoque < (int)dto.Quantidade)
                    return ServiceResult<OrdemServicoDto>.BadRequest(
                        $"Estoque insuficiente para a peça '{peca.Nome}'. Disponível: {peca.QuantidadeEstoque}, solicitado: {dto.Quantidade}.");

                peca.QuantidadeEstoque -= (int)dto.Quantidade;
                peca.DataAtualizacao = DateTime.UtcNow;

                if (peca.QuantidadeEstoque < peca.QuantidadeMinima)
                    _logger.LogWarning("Peça {PecaId} ({Nome}) abaixo do estoque mínimo: {Atual}/{Minimo}",
                        peca.Id, peca.Nome, peca.QuantidadeEstoque, peca.QuantidadeMinima);

                precoUnitario = peca.PrecoUnitario;
            }

            var item = new ItemOrdemServico
            {
                OrdemServicoId = ordemServicoId,
                Tipo = dto.Tipo,
                ServicoId = dto.Tipo == TipoItemOrdemServico.Servico ? dto.ServicoId : null,
                PecaId = dto.Tipo == TipoItemOrdemServico.Peca ? dto.PecaId : null,
                Quantidade = dto.Quantidade,
                PrecoUnitario = precoUnitario,
                PrecoTotal = precoUnitario * dto.Quantidade,
                DataCriacao = DateTime.UtcNow
            };

            // Capture existing total BEFORE Add() so EF change tracker doesn't double-count
            var existingTotal = ordem.Itens.Sum(i => i.PrecoTotal);
            _db.ItensOrdemServico.Add(item);

            ordem.ValorTotal = existingTotal + item.PrecoTotal;
            ordem.DataAtualizacao = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var ordemAtualizada = await FindWithIncludes(ordemServicoId);
            return ServiceResult<OrdemServicoDto>.Created(ToDto(ordemAtualizada!));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult<OrdemServicoDto>> DeleteItemAsync(int ordemServicoId, int itemId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var ordem = await FindWithIncludes(ordemServicoId);
            if (ordem is null)
                return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {ordemServicoId} não encontrada.");

            var item = ordem.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item is null)
                return ServiceResult<OrdemServicoDto>.NotFound($"Item {itemId} não encontrado na OS {ordemServicoId}.");

            if (item.Tipo == TipoItemOrdemServico.Peca && item.PecaId.HasValue)
            {
                var peca = await _db.Pecas.FindAsync(item.PecaId.Value);
                if (peca is not null)
                {
                    peca.QuantidadeEstoque += (int)item.Quantidade;
                    peca.DataAtualizacao = DateTime.UtcNow;
                }
            }

            _db.ItensOrdemServico.Remove(item);

            ordem.ValorTotal = ordem.Itens
                .Where(i => i.Id != itemId)
                .Sum(i => i.PrecoTotal);
            ordem.DataAtualizacao = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var ordemAtualizada = await FindWithIncludes(ordemServicoId);
            return ServiceResult<OrdemServicoDto>.Ok(ToDto(ordemAtualizada!));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<string> GerarNumeroOSAsync()
    {
        var ano = DateTime.UtcNow.Year;
        var prefixo = $"OS-{ano}-";

        var ultimo = await _db.OrdensServico
            .Where(o => o.NumeroOS.StartsWith(prefixo))
            .OrderByDescending(o => o.NumeroOS)
            .Select(o => o.NumeroOS)
            .FirstOrDefaultAsync();

        int seq = 1;
        if (ultimo is not null)
        {
            var partes = ultimo.Split('-');
            if (partes.Length == 3 && int.TryParse(partes[2], out var ultimoSeq))
                seq = ultimoSeq + 1;
        }

        return $"{prefixo}{seq:D6}";
    }

    private static (bool valid, string? error) ValidateTransition(OrdemServico ordem, StatusOrdemServico novoStatus)
    {
        return (ordem.Status, novoStatus) switch
        {
            (StatusOrdemServico.Recebida, StatusOrdemServico.EmDiagnostico) => (true, null),
            (StatusOrdemServico.EmDiagnostico, StatusOrdemServico.AguardandoAprovacao) => (true, null),
            (StatusOrdemServico.AguardandoAprovacao, StatusOrdemServico.EmExecucao) when ordem.OrcamentoAprovado => (true, null),
            (StatusOrdemServico.AguardandoAprovacao, StatusOrdemServico.EmExecucao) => (false, "Orçamento não aprovado. Aprovação necessária antes de iniciar a execução."),
            (StatusOrdemServico.EmExecucao, StatusOrdemServico.Finalizada) => (true, null),
            (StatusOrdemServico.Finalizada, StatusOrdemServico.Entregue) => (true, null),
            _ => (false, $"Transição inválida: {ordem.Status} → {novoStatus}.")
        };
    }

    private async Task<OrdemServico?> FindWithIncludes(int id) =>
        await _db.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Veiculo)
            .Include(o => o.Itens).ThenInclude(i => i.Servico)
            .Include(o => o.Itens).ThenInclude(i => i.Peca)
            .FirstOrDefaultAsync(o => o.Id == id);

    private static OrdemServicoDto ToDto(OrdemServico o) => new(
        o.Id,
        o.NumeroOS,
        o.ClienteId,
        o.Cliente?.Nome ?? string.Empty,
        o.VeiculoId,
        o.Veiculo?.Placa ?? string.Empty,
        o.Status,
        o.Descricao,
        o.ValorTotal,
        o.OrcamentoAprovado,
        o.DataAprovacaoOrcamento,
        o.DataPrevisaoTermino,
        o.DataFinalizacao,
        o.DataEntrega,
        o.DataCriacao,
        o.DataAtualizacao,
        o.Itens.Select(i => new ItemOrdemServicoDto(
            i.Id,
            i.Tipo,
            i.ServicoId,
            i.Servico?.Nome,
            i.PecaId,
            i.Peca?.Nome,
            i.Quantidade,
            i.PrecoUnitario,
            i.PrecoTotal,
            i.DataCriacao
        ))
    );
}
