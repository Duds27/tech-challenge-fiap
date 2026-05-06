namespace OficinaMecanicaBackend.DTOs.Veiculos;

public record CreateVeiculoDto(
    string Placa,
    string Marca,
    string Modelo,
    int Ano,
    string? Cor,
    string? VinNumber,
    int ClienteId
);

public record UpdateVeiculoDto(
    string Marca,
    string Modelo,
    int Ano,
    string? Cor,
    string? VinNumber
);

public record VeiculoDto(
    int Id,
    string Placa,
    string Marca,
    string Modelo,
    int Ano,
    string? Cor,
    string? VinNumber,
    int ClienteId,
    DateTime DataCriacao,
    DateTime? DataAtualizacao
);
