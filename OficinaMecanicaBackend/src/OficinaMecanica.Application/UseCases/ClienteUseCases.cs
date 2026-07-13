using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Application.Common;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.Validators;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases;

public class ClienteUseCases
{
    private readonly IClienteRepository _clientes;
    private readonly IOrdemServicoRepository _ordens;
    private readonly IUnitOfWork _uow;

    public ClienteUseCases(IClienteRepository clientes, IOrdemServicoRepository ordens, IUnitOfWork uow)
    {
        _clientes = clientes;
        _ordens = ordens;
        _uow = uow;
    }

    public async Task<ServiceResult<IEnumerable<ClienteDto>>> ListarAsync()
    {
        var clientes = await _clientes.ListarAsync();
        return ServiceResult<IEnumerable<ClienteDto>>.Ok(clientes.Select(ToDto));
    }

    public async Task<ServiceResult<ClienteDto>> ObterAsync(int id)
    {
        var cliente = await _clientes.ObterAsync(id);
        if (cliente is null)
            return ServiceResult<ClienteDto>.NotFound($"Cliente {id} não encontrado.");
        return ServiceResult<ClienteDto>.Ok(ToDto(cliente));
    }

    public async Task<ServiceResult<ClienteDto>> CriarAsync(CreateClienteDto dto)
    {
        if (!CpfCnpjValidator.IsValid(dto.CpfCnpj))
            return ServiceResult<ClienteDto>.BadRequest("CPF/CNPJ inválido.");

        var cpfCnpjNormalizado = Normalizar(dto.CpfCnpj);

        if (await _clientes.ExistePorCpfCnpjAsync(cpfCnpjNormalizado))
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

        await _clientes.AdicionarAsync(cliente);
        await _uow.SalvarAsync();
        return ServiceResult<ClienteDto>.Created(ToDto(cliente));
    }

    public async Task<ServiceResult<ClienteDto>> AtualizarAsync(int id, UpdateClienteDto dto)
    {
        var cliente = await _clientes.ObterAsync(id, tracking: true);
        if (cliente is null)
            return ServiceResult<ClienteDto>.NotFound($"Cliente {id} não encontrado.");

        cliente.Nome = dto.Nome;
        cliente.Email = dto.Email;
        cliente.Telefone = dto.Telefone;
        cliente.Endereco = dto.Endereco;
        cliente.Cidade = dto.Cidade;
        cliente.Estado = dto.Estado;
        cliente.DataAtualizacao = DateTime.UtcNow;

        await _uow.SalvarAsync();
        return ServiceResult<ClienteDto>.Ok(ToDto(cliente));
    }

    public async Task<ServiceResult<object>> ExcluirAsync(int id)
    {
        var cliente = await _clientes.ObterAsync(id, tracking: true);
        if (cliente is null)
            return ServiceResult<object>.NotFound($"Cliente {id} não encontrado.");

        if (await _ordens.ExistePorClienteAsync(id))
            return ServiceResult<object>.Conflict("Cliente possui ordens de serviço e não pode ser excluído.");

        _clientes.Remover(cliente);
        await _uow.SalvarAsync();
        return ServiceResult<object>.Ok(new { });
    }

    private static string Normalizar(string cpfCnpj) =>
        cpfCnpj.ToUpperInvariant().Replace(".", "").Replace("-", "").Replace("/", "");

    private static ClienteDto ToDto(Cliente c) => new(
        c.Id, c.CpfCnpj, c.Nome, c.Email, c.Telefone,
        c.Endereco, c.Cidade, c.Estado, c.DataCriacao, c.DataAtualizacao
    );
}
