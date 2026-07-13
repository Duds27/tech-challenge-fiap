using OficinaMecanicaBackend.Models.Enums;

namespace OficinaMecanicaBackend.DTOs.OrdensServico;

public record CreateOrdemServicoDto(
    int ClienteId,
    int VeiculoId,
    string? Descricao,
    DateTime? DataPrevisaoTermino
);

public record UpdateStatusDto(StatusOrdemServico NovoStatus);

public record AprovarOrcamentoDto(bool Aprovado);

public record AddItemDto(
    TipoItemOrdemServico Tipo,
    int? ServicoId,
    int? PecaId,
    decimal Quantidade
);

public record ItemOrdemServicoDto(
    int Id,
    TipoItemOrdemServico Tipo,
    int? ServicoId,
    string? ServicoNome,
    int? PecaId,
    string? PecaNome,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal PrecoTotal,
    DateTime DataCriacao
);

public record OrdemServicoDto(
    int Id,
    string NumeroOS,
    int ClienteId,
    string ClienteNome,
    int VeiculoId,
    string VeiculoPlaca,
    StatusOrdemServico Status,
    string? Descricao,
    decimal ValorTotal,
    bool OrcamentoAprovado,
    DateTime? DataAprovacaoOrcamento,
    DateTime? DataPrevisaoTermino,
    DateTime? DataFinalizacao,
    DateTime? DataEntrega,
    DateTime DataCriacao,
    DateTime? DataAtualizacao,
    IEnumerable<ItemOrdemServicoDto> Itens
);

public record StatusPublicoDto(
    string NumeroOS,
    StatusOrdemServico Status,
    string StatusDescricao,
    DateTime? DataPrevisaoTermino
);

/// <summary>
/// Tempo médio de execução das ordens de serviço, medido do momento de
/// criação (DataCriacao) até a finalização (DataFinalizacao). Apenas ordens
/// já finalizadas são consideradas no cálculo.
/// </summary>
public record TempoMedioExecucaoDto(
    int OrdensConsideradas,
    double TempoMedioSegundos,
    double TempoMedioMinutos,
    double TempoMedioHoras,
    string TempoMedioFormatado
);
