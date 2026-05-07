namespace OficinaMecanicaBackend.DTOs.Clientes;

public record CreateClienteDto(
    string CpfCnpj,
    string Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    string? Cidade,
    string? Estado
);

public record UpdateClienteDto(
    string Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    string? Cidade,
    string? Estado
);

public record ClienteDto(
    int Id,
    string CpfCnpj,
    string Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    string? Cidade,
    string? Estado,
    DateTime DataCriacao,
    DateTime? DataAtualizacao
);
