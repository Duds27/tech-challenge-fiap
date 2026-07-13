namespace OficinaMecanica.Application.DTOs.Pecas;

public record CreatePecaDto(
    string Nome,
    string? Descricao,
    string? Codigo,
    decimal PrecoUnitario,
    int QuantidadeEstoque,
    int QuantidadeMinima,
    bool Ativo
);

public record UpdatePecaDto(
    string Nome,
    string? Descricao,
    string? Codigo,
    decimal PrecoUnitario,
    int QuantidadeEstoque,
    int QuantidadeMinima,
    bool Ativo
);

public record PecaDto(
    int Id,
    string Nome,
    string? Descricao,
    string? Codigo,
    decimal PrecoUnitario,
    int QuantidadeEstoque,
    int QuantidadeMinima,
    bool Ativo,
    bool EstoqueBaixo,
    DateTime DataCriacao,
    DateTime? DataAtualizacao
);
