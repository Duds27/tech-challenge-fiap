using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Application.Common;
using OficinaMecanica.Application.DTOs.Veiculos;
using OficinaMecanica.Application.Validators;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases;

public class VeiculoUseCases
{
    private readonly IVeiculoRepository _veiculos;
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _uow;

    public VeiculoUseCases(IVeiculoRepository veiculos, IClienteRepository clientes, IUnitOfWork uow)
    {
        _veiculos = veiculos;
        _clientes = clientes;
        _uow = uow;
    }

    public async Task<ServiceResult<IEnumerable<VeiculoDto>>> ListarAsync()
    {
        var veiculos = await _veiculos.ListarAsync();
        return ServiceResult<IEnumerable<VeiculoDto>>.Ok(veiculos.Select(ToDto));
    }

    public async Task<ServiceResult<VeiculoDto>> ObterAsync(int id)
    {
        var veiculo = await _veiculos.ObterAsync(id);
        if (veiculo is null)
            return ServiceResult<VeiculoDto>.NotFound($"Veículo {id} não encontrado.");
        return ServiceResult<VeiculoDto>.Ok(ToDto(veiculo));
    }

    public async Task<ServiceResult<IEnumerable<VeiculoDto>>> ListarPorClienteAsync(int clienteId)
    {
        if (!await _clientes.ExisteAsync(clienteId))
            return ServiceResult<IEnumerable<VeiculoDto>>.NotFound($"Cliente {clienteId} não encontrado.");

        var veiculos = await _veiculos.ListarPorClienteAsync(clienteId);
        return ServiceResult<IEnumerable<VeiculoDto>>.Ok(veiculos.Select(ToDto));
    }

    public async Task<ServiceResult<VeiculoDto>> CriarAsync(CreateVeiculoDto dto)
    {
        if (!PlacaValidator.IsValid(dto.Placa))
            return ServiceResult<VeiculoDto>.BadRequest("Placa inválida.");

        if (!await _clientes.ExisteAsync(dto.ClienteId))
            return ServiceResult<VeiculoDto>.NotFound($"Cliente {dto.ClienteId} não encontrado.");

        var placaNormalizada = dto.Placa.ToUpperInvariant().Replace("-", "");
        if (await _veiculos.ExistePorPlacaAsync(placaNormalizada))
            return ServiceResult<VeiculoDto>.Conflict("Placa já cadastrada.");

        var veiculo = new Veiculo
        {
            Placa = placaNormalizada,
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            Ano = dto.Ano,
            Cor = dto.Cor,
            VinNumber = dto.VinNumber,
            ClienteId = dto.ClienteId,
            DataCriacao = DateTime.UtcNow
        };

        await _veiculos.AdicionarAsync(veiculo);
        await _uow.SalvarAsync();
        return ServiceResult<VeiculoDto>.Created(ToDto(veiculo));
    }

    public async Task<ServiceResult<VeiculoDto>> AtualizarAsync(int id, UpdateVeiculoDto dto)
    {
        var veiculo = await _veiculos.ObterAsync(id, tracking: true);
        if (veiculo is null)
            return ServiceResult<VeiculoDto>.NotFound($"Veículo {id} não encontrado.");

        veiculo.Marca = dto.Marca;
        veiculo.Modelo = dto.Modelo;
        veiculo.Ano = dto.Ano;
        veiculo.Cor = dto.Cor;
        veiculo.VinNumber = dto.VinNumber;
        veiculo.DataAtualizacao = DateTime.UtcNow;

        await _uow.SalvarAsync();
        return ServiceResult<VeiculoDto>.Ok(ToDto(veiculo));
    }

    public async Task<ServiceResult<object>> ExcluirAsync(int id)
    {
        var veiculo = await _veiculos.ObterAsync(id, tracking: true);
        if (veiculo is null)
            return ServiceResult<object>.NotFound($"Veículo {id} não encontrado.");

        _veiculos.Remover(veiculo);
        await _uow.SalvarAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static VeiculoDto ToDto(Veiculo v) => new(
        v.Id, v.Placa, v.Marca, v.Modelo, v.Ano, v.Cor, v.VinNumber,
        v.ClienteId, v.DataCriacao, v.DataAtualizacao
    );
}
