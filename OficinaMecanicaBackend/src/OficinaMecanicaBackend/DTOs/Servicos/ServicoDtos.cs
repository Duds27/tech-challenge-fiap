namespace OficinaMecanicaBackend.DTOs.Servicos;

public record CreateServicoDto(
    string Nome,
    string? Descricao,
    decimal PrecoBase,
    decimal? TempoEstimadoHoras,
    bool Ativo
);

public record UpdateServicoDto(
    string Nome,
    string? Descricao,
    decimal PrecoBase,
    decimal? TempoEstimadoHoras,
    bool Ativo
);

public record ServicoDto(
    int Id,
    string Nome,
    string? Descricao,
    decimal PrecoBase,
    decimal? TempoEstimadoHoras,
    bool Ativo,
    DateTime DataCriacao,
    DateTime? DataAtualizacao
);
