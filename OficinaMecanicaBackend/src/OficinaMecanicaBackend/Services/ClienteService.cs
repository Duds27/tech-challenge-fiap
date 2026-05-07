using Microsoft.EntityFrameworkCore;
using OficinaMecanicaBackend.Data;
using OficinaMecanicaBackend.DTOs.Clientes;
using OficinaMecanicaBackend.Models;
using OficinaMecanicaBackend.Validators;

namespace OficinaMecanicaBackend.Services;

public class ClienteService
{
    private readonly AppDbContext _db;

    public ClienteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<IEnumerable<ClienteDto>>> GetAllAsync()
    {
        var clientes = await _db.Clientes.AsNoTracking().ToListAsync();
        return ServiceResult<IEnumerable<ClienteDto>>.Ok(clientes.Select(ToDto));
    }

    public async Task<ServiceResult<ClienteDto>> GetByIdAsync(int id)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
            return ServiceResult<ClienteDto>.NotFound($"Cliente {id} não encontrado.");
        return ServiceResult<ClienteDto>.Ok(ToDto(cliente));
    }

    public async Task<ServiceResult<ClienteDto>> CreateAsync(CreateClienteDto dto)
    {
        if (!CpfCnpjValidator.IsValid(dto.CpfCnpj))
            return ServiceResult<ClienteDto>.BadRequest("CPF/CNPJ inválido.");

        var cpfCnpjNormalizado = dto.CpfCnpj.ToUpperInvariant()
            .Replace(".", "").Replace("-", "").Replace("/", "");

        var existe = await _db.Clientes.AnyAsync(c => c.CpfCnpj == cpfCnpjNormalizado);
        if (existe)
            return ServiceResult<ClienteDto>.Conflict("CPF/CNPJ já cadastrado.");

        var cliente = new Cliente
        {
            CpfCnpj = cpfCnpjNormalizado,
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            Endereco = dto.Endereco,
            Cidade = dto.Cidade,
            Estado = dto.Estado,
            DataCriacao = DateTime.UtcNow
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();
        return ServiceResult<ClienteDto>.Created(ToDto(cliente));
    }

    public async Task<ServiceResult<ClienteDto>> UpdateAsync(int id, UpdateClienteDto dto)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null)
            return ServiceResult<ClienteDto>.NotFound($"Cliente {id} não encontrado.");

        cliente.Nome = dto.Nome;
        cliente.Email = dto.Email;
        cliente.Telefone = dto.Telefone;
        cliente.Endereco = dto.Endereco;
        cliente.Cidade = dto.Cidade;
        cliente.Estado = dto.Estado;
        cliente.DataAtualizacao = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult<ClienteDto>.Ok(ToDto(cliente));
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null)
            return ServiceResult<object>.NotFound($"Cliente {id} não encontrado.");

        var temOS = await _db.OrdensServico.AnyAsync(o => o.ClienteId == id);
        if (temOS)
            return ServiceResult<object>.Conflict("Cliente possui ordens de serviço e não pode ser excluído.");

        _db.Clientes.Remove(cliente);
        await _db.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static ClienteDto ToDto(Cliente c) => new(
        c.Id, c.CpfCnpj, c.Nome, c.Email, c.Telefone,
        c.Endereco, c.Cidade, c.Estado, c.DataCriacao, c.DataAtualizacao
    );
}
