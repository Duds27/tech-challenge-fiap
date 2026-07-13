using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Abstractions;

public interface IClienteRepository
{
    Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken ct = default);
    Task<Cliente?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default);
    Task<bool> ExisteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistePorCpfCnpjAsync(string cpfCnpj, CancellationToken ct = default);
    Task AdicionarAsync(Cliente cliente, CancellationToken ct = default);
    void Remover(Cliente cliente);
}

public interface IVeiculoRepository
{
    Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Veiculo>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default);
    Task<Veiculo?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default);
    Task<bool> ExistePorPlacaAsync(string placa, CancellationToken ct = default);
    Task<bool> PertenceAoClienteAsync(int veiculoId, int clienteId, CancellationToken ct = default);
    Task AdicionarAsync(Veiculo veiculo, CancellationToken ct = default);
    void Remover(Veiculo veiculo);
}

public interface IPecaRepository
{
    Task<IReadOnlyList<Peca>> ListarAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Peca>> ListarEstoqueBaixoAsync(CancellationToken ct = default);
    Task<Peca?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default);
    Task<bool> ExistePorCodigoAsync(string codigo, int? ignorarId = null, CancellationToken ct = default);
    Task AdicionarAsync(Peca peca, CancellationToken ct = default);
    void Remover(Peca peca);
}

public interface IServicoRepository
{
    Task<IReadOnlyList<Servico>> ListarAsync(CancellationToken ct = default);
    Task<Servico?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default);
    Task AdicionarAsync(Servico servico, CancellationToken ct = default);
    void Remover(Servico servico);
}

public interface IOrdemServicoRepository
{
    Task<IReadOnlyList<OrdemServico>> ListarComIncludesAsync(CancellationToken ct = default);
    Task<OrdemServico?> ObterComIncludesAsync(int id, bool tracking = true, CancellationToken ct = default);
    Task<OrdemServico?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default);
    Task<bool> ExistePorClienteAsync(int clienteId, CancellationToken ct = default);
    Task<string?> UltimoNumeroOSAsync(string prefixo, CancellationToken ct = default);
    Task<IReadOnlyList<OrdemServico>> ListarFinalizadasComExecucaoAsync(CancellationToken ct = default);
    Task AdicionarAsync(OrdemServico ordem, CancellationToken ct = default);
    Task AdicionarItemAsync(ItemOrdemServico item, CancellationToken ct = default);
    void RemoverItem(ItemOrdemServico item);
}
