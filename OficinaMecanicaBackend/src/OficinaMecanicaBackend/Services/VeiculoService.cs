using Microsoft.EntityFrameworkCore;
using OficinaMecanicaBackend.Data;
using OficinaMecanicaBackend.DTOs.Veiculos;
using OficinaMecanicaBackend.Models;
using OficinaMecanicaBackend.Validators;

namespace OficinaMecanicaBackend.Services;

public class VeiculoService
{
    private readonly AppDbContext _db;

    public VeiculoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<IEnumerable<VeiculoDto>>> GetAllAsync()
    {
        var veiculos = await _db.Veiculos.AsNoTracking().ToListAsync();
        return ServiceResult<IEnumerable<VeiculoDto>>.Ok(veiculos.Select(ToDto));
    }

    public async Task<ServiceResult<VeiculoDto>> GetByIdAsync(int id)
    {
        var veiculo = await _db.Veiculos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        if (veiculo is null)
            return ServiceResult<VeiculoDto>.NotFound($"Veículo {id} não encontrado.");
        return ServiceResult<VeiculoDto>.Ok(ToDto(veiculo));
    }

    public async Task<ServiceResult<IEnumerable<VeiculoDto>>> GetByClienteIdAsync(int clienteId)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == clienteId);
        if (!clienteExiste)
            return ServiceResult<IEnumerable<VeiculoDto>>.NotFound($"Cliente {clienteId} não encontrado.");

        var veiculos = await _db.Veiculos.AsNoTracking()
            .Where(v => v.ClienteId == clienteId)
            .ToListAsync();
        return ServiceResult<IEnumerable<VeiculoDto>>.Ok(veiculos.Select(ToDto));
    }

    public async Task<ServiceResult<VeiculoDto>> CreateAsync(CreateVeiculoDto dto)
    {
        if (!PlacaValidator.IsValid(dto.Placa))
            return ServiceResult<VeiculoDto>.BadRequest("Placa inválida.");

        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId);
        if (!clienteExiste)
            return ServiceResult<VeiculoDto>.NotFound($"Cliente {dto.ClienteId} não encontrado.");

        var placaNormalizada = dto.Placa.ToUpperInvariant().Replace("-", "");
        var placaExiste = await _db.Veiculos.AnyAsync(v => v.Placa == placaNormalizada);
        if (placaExiste)
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

        _db.Veiculos.Add(veiculo);
        await _db.SaveChangesAsync();
        return ServiceResult<VeiculoDto>.Created(ToDto(veiculo));
    }

    public async Task<ServiceResult<VeiculoDto>> UpdateAsync(int id, UpdateVeiculoDto dto)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo is null)
            return ServiceResult<VeiculoDto>.NotFound($"Veículo {id} não encontrado.");

        veiculo.Marca = dto.Marca;
        veiculo.Modelo = dto.Modelo;
        veiculo.Ano = dto.Ano;
        veiculo.Cor = dto.Cor;
        veiculo.VinNumber = dto.VinNumber;
        veiculo.DataAtualizacao = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult<VeiculoDto>.Ok(ToDto(veiculo));
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo is null)
            return ServiceResult<object>.NotFound($"Veículo {id} não encontrado.");

        _db.Veiculos.Remove(veiculo);
        await _db.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static VeiculoDto ToDto(Veiculo v) => new(
        v.Id, v.Placa, v.Marca, v.Modelo, v.Ano, v.Cor, v.VinNumber,
        v.ClienteId, v.DataCriacao, v.DataAtualizacao
    );
}
