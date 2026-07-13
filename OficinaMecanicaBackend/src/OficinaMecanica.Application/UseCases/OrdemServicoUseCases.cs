using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Application.Common;
using OficinaMecanica.Application.DTOs.OrdensServico;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases;

public class OrdemServicoUseCases
{
    private readonly IOrdemServicoRepository _ordens;
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;
    private readonly IPecaRepository _pecas;
    private readonly IServicoRepository _servicos;
    private readonly IUnitOfWork _uow;
    private readonly INotificadorStatus _notificador;
    private readonly ILogger<OrdemServicoUseCases> _logger;

    public OrdemServicoUseCases(
        IOrdemServicoRepository ordens,
        IClienteRepository clientes,
        IVeiculoRepository veiculos,
        IPecaRepository pecas,
        IServicoRepository servicos,
        IUnitOfWork uow,
        INotificadorStatus notificador,
        ILogger<OrdemServicoUseCases> logger)
    {
        _ordens = ordens;
        _clientes = clientes;
        _veiculos = veiculos;
        _pecas = pecas;
        _servicos = servicos;
        _uow = uow;
        _notificador = notificador;
        _logger = logger;
    }

    /// <summary>
    /// Lista as OS ativas ordenadas por prioridade de status
    /// (Em Execução &gt; Aguardando Aprovação &gt; Em Diagnóstico &gt; Recebida) e,
    /// dentro do mesmo status, das mais antigas para as mais recentes. OS
    /// finalizadas e entregues são omitidas (exclusão lógica), salvo se
    /// <paramref name="incluirConcluidas"/> for verdadeiro.
    /// </summary>
    public async Task<ServiceResult<IEnumerable<OrdemServicoDto>>> ListarAsync(bool incluirConcluidas = false)
    {
        var ordens = await _ordens.ListarComIncludesAsync();

        var filtradas = incluirConcluidas
            ? ordens
            : ordens.Where(o => o.VisivelNaListagem);

        var ordenadas = filtradas
            .OrderBy(o => OrdemServico.PrioridadeListagem(o.Status))
            .ThenBy(o => o.DataCriacao)
            .Select(ToDto);

        return ServiceResult<IEnumerable<OrdemServicoDto>>.Ok(ordenadas);
    }

    public async Task<ServiceResult<OrdemServicoDto>> ObterAsync(int id)
    {
        var ordem = await _ordens.ObterComIncludesAsync(id, tracking: false);
        if (ordem is null)
            return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");
        return ServiceResult<OrdemServicoDto>.Ok(ToDto(ordem));
    }

    public async Task<ServiceResult<StatusPublicoDto>> ObterStatusPublicoAsync(int id)
    {
        var ordem = await _ordens.ObterAsync(id);
        if (ordem is null)
            return ServiceResult<StatusPublicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");

        return ServiceResult<StatusPublicoDto>.Ok(new StatusPublicoDto(
            ordem.NumeroOS,
            ordem.Status,
            ordem.Status.ToString(),
            ordem.DataPrevisaoTermino
        ));
    }

    public async Task<ServiceResult<TempoMedioExecucaoDto>> CalcularTempoMedioExecucaoAsync()
    {
        var ordens = await _ordens.ListarFinalizadasComExecucaoAsync();

        if (ordens.Count == 0)
            return ServiceResult<TempoMedioExecucaoDto>.Ok(
                new TempoMedioExecucaoDto(0, 0, 0, 0, "00:00:00"));

        var mediaSegundos = ordens.Average(o =>
            (o.DataFinalizacao!.Value - o.DataInicioExecucao!.Value).TotalSeconds);
        var media = TimeSpan.FromSeconds(mediaSegundos);

        return ServiceResult<TempoMedioExecucaoDto>.Ok(new TempoMedioExecucaoDto(
            ordens.Count,
            Math.Round(mediaSegundos, 2),
            Math.Round(media.TotalMinutes, 2),
            Math.Round(media.TotalHours, 2),
            $"{(int)media.TotalHours:D2}:{media.Minutes:D2}:{media.Seconds:D2}"
        ));
    }

    public async Task<ServiceResult<OrdemServicoDto>> CriarAsync(CreateOrdemServicoDto dto)
    {
        if (!await _clientes.ExisteAsync(dto.ClienteId))
            return ServiceResult<OrdemServicoDto>.NotFound($"Cliente {dto.ClienteId} não encontrado.");

        if (!await _veiculos.PertenceAoClienteAsync(dto.VeiculoId, dto.ClienteId))
            return ServiceResult<OrdemServicoDto>.NotFound($"Veículo {dto.VeiculoId} não encontrado ou não pertence ao cliente.");

        var ordem = new OrdemServico
        {
            NumeroOS = await GerarNumeroOSAsync(),
            ClienteId = dto.ClienteId,
            VeiculoId = dto.VeiculoId,
            Status = StatusOrdemServico.Recebida,
            Descricao = dto.Descricao,
            ValorTotal = 0,
            OrcamentoAprovado = false,
            DataPrevisaoTermino = dto.DataPrevisaoTermino,
            DataCriacao = DateTime.UtcNow
        };

        await _ordens.AdicionarAsync(ordem);
        try
        {
            await _uow.SalvarAsync();
        }
        catch (ConflitoPersistenciaException)
        {
            // Retry once on NumeroOS unique constraint collision (race condition)
            ordem.NumeroOS = await GerarNumeroOSAsync();
            await _uow.SalvarAsync();
        }

        var ordemComIncludes = await _ordens.ObterComIncludesAsync(ordem.Id, tracking: false);
        return ServiceResult<OrdemServicoDto>.Created(ToDto(ordemComIncludes!));
    }

    public async Task<ServiceResult<OrdemServicoDto>> AvancarStatusAsync(int id, UpdateStatusDto dto)
    {
        var ordem = await _ordens.ObterComIncludesAsync(id, tracking: true);
        if (ordem is null)
            return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");

        var (valid, error) = ordem.ValidarTransicao(dto.NovoStatus);
        if (!valid)
            return ServiceResult<OrdemServicoDto>.BadRequest(error!);

        var statusAnterior = ordem.Status;
        ordem.AplicarStatus(dto.NovoStatus, DateTime.UtcNow);
        await _uow.SalvarAsync();

        await _notificador.NotificarMudancaStatusAsync(ordem, statusAnterior, dto.NovoStatus);

        return ServiceResult<OrdemServicoDto>.Ok(ToDto(ordem));
    }

    public async Task<ServiceResult<OrdemServicoDto>> AprovarOrcamentoAsync(int id, AprovarOrcamentoDto dto)
    {
        var ordem = await _ordens.ObterComIncludesAsync(id, tracking: true);
        if (ordem is null)
            return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {id} não encontrada.");

        if (ordem.Status != StatusOrdemServico.AguardandoAprovacao)
            return ServiceResult<OrdemServicoDto>.BadRequest(
                "Aprovação de orçamento só é possível quando o status é 'AguardandoAprovacao'.");

        ordem.OrcamentoAprovado = dto.Aprovado;
        ordem.DataAprovacaoOrcamento = DateTime.UtcNow;
        ordem.DataAtualizacao = DateTime.UtcNow;

        await _uow.SalvarAsync();
        return ServiceResult<OrdemServicoDto>.Ok(ToDto(ordem));
    }

    public async Task<ServiceResult<OrdemServicoDto>> AdicionarItemAsync(int ordemServicoId, AddItemDto dto)
    {
        await using var tx = await _uow.IniciarTransacaoAsync();
        try
        {
            var ordem = await _ordens.ObterComIncludesAsync(ordemServicoId, tracking: true);
            if (ordem is null)
                return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {ordemServicoId} não encontrada.");

            if (ordem.Status is StatusOrdemServico.Finalizada or StatusOrdemServico.Entregue)
                return ServiceResult<OrdemServicoDto>.BadRequest("Não é possível adicionar itens a uma OS finalizada ou entregue.");

            decimal precoUnitario;

            if (dto.Tipo == TipoItemOrdemServico.Servico)
            {
                var servico = await _servicos.ObterAsync(dto.ServicoId!.Value, tracking: true);
                if (servico is null)
                    return ServiceResult<OrdemServicoDto>.NotFound($"Serviço {dto.ServicoId} não encontrado.");
                precoUnitario = servico.PrecoBase;
            }
            else
            {
                var peca = await _pecas.ObterAsync(dto.PecaId!.Value, tracking: true);
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

            // Capture existing total BEFORE Add() so o change tracker não conte em dobro
            var existingTotal = ordem.Itens.Sum(i => i.PrecoTotal);
            await _ordens.AdicionarItemAsync(item);

            ordem.ValorTotal = existingTotal + item.PrecoTotal;
            ordem.DataAtualizacao = DateTime.UtcNow;

            await _uow.SalvarAsync();
            await tx.CommitAsync();

            var ordemAtualizada = await _ordens.ObterComIncludesAsync(ordemServicoId, tracking: false);
            return ServiceResult<OrdemServicoDto>.Created(ToDto(ordemAtualizada!));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult<OrdemServicoDto>> RemoverItemAsync(int ordemServicoId, int itemId)
    {
        await using var tx = await _uow.IniciarTransacaoAsync();
        try
        {
            var ordem = await _ordens.ObterComIncludesAsync(ordemServicoId, tracking: true);
            if (ordem is null)
                return ServiceResult<OrdemServicoDto>.NotFound($"Ordem de serviço {ordemServicoId} não encontrada.");

            var item = ordem.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item is null)
                return ServiceResult<OrdemServicoDto>.NotFound($"Item {itemId} não encontrado na OS {ordemServicoId}.");

            if (item.Tipo == TipoItemOrdemServico.Peca && item.PecaId.HasValue)
            {
                var peca = await _pecas.ObterAsync(item.PecaId.Value, tracking: true);
                if (peca is not null)
                {
                    peca.QuantidadeEstoque += (int)item.Quantidade;
                    peca.DataAtualizacao = DateTime.UtcNow;
                }
            }

            _ordens.RemoverItem(item);

            ordem.ValorTotal = ordem.Itens
                .Where(i => i.Id != itemId)
                .Sum(i => i.PrecoTotal);
            ordem.DataAtualizacao = DateTime.UtcNow;

            await _uow.SalvarAsync();
            await tx.CommitAsync();

            var ordemAtualizada = await _ordens.ObterComIncludesAsync(ordemServicoId, tracking: false);
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

        var ultimo = await _ordens.UltimoNumeroOSAsync(prefixo);

        int seq = 1;
        if (ultimo is not null)
        {
            var partes = ultimo.Split('-');
            if (partes.Length == 3 && int.TryParse(partes[2], out var ultimoSeq))
                seq = ultimoSeq + 1;
        }

        return $"{prefixo}{seq:D6}";
    }

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
        o.DataInicioExecucao,
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
