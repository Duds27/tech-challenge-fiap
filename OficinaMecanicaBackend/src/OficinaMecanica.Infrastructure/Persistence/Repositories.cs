using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Infrastructure.Data;

namespace OficinaMecanica.Infrastructure.Persistence;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _db;
    public ClienteRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken ct = default) =>
        await _db.Clientes.AsNoTracking().ToListAsync(ct);

    public async Task<Cliente?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default)
    {
        var query = tracking ? _db.Clientes : _db.Clientes.AsNoTracking();
        return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public Task<bool> ExisteAsync(int id, CancellationToken ct = default) =>
        _db.Clientes.AnyAsync(c => c.Id == id, ct);

    public Task<bool> ExistePorCpfCnpjAsync(string cpfCnpj, CancellationToken ct = default) =>
        _db.Clientes.AnyAsync(c => c.CpfCnpj == cpfCnpj, ct);

    public async Task AdicionarAsync(Cliente cliente, CancellationToken ct = default) =>
        await _db.Clientes.AddAsync(cliente, ct);

    public void Remover(Cliente cliente) => _db.Clientes.Remove(cliente);
}

public class VeiculoRepository : IVeiculoRepository
{
    private readonly AppDbContext _db;
    public VeiculoRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken ct = default) =>
        await _db.Veiculos.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Veiculo>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default) =>
        await _db.Veiculos.AsNoTracking().Where(v => v.ClienteId == clienteId).ToListAsync(ct);

    public async Task<Veiculo?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default)
    {
        var query = tracking ? _db.Veiculos : _db.Veiculos.AsNoTracking();
        return await query.FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public Task<bool> ExistePorPlacaAsync(string placa, CancellationToken ct = default) =>
        _db.Veiculos.AnyAsync(v => v.Placa == placa, ct);

    public Task<bool> PertenceAoClienteAsync(int veiculoId, int clienteId, CancellationToken ct = default) =>
        _db.Veiculos.AnyAsync(v => v.Id == veiculoId && v.ClienteId == clienteId, ct);

    public async Task AdicionarAsync(Veiculo veiculo, CancellationToken ct = default) =>
        await _db.Veiculos.AddAsync(veiculo, ct);

    public void Remover(Veiculo veiculo) => _db.Veiculos.Remove(veiculo);
}

public class PecaRepository : IPecaRepository
{
    private readonly AppDbContext _db;
    public PecaRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Peca>> ListarAsync(CancellationToken ct = default) =>
        await _db.Pecas.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Peca>> ListarEstoqueBaixoAsync(CancellationToken ct = default) =>
        await _db.Pecas.AsNoTracking()
            .Where(p => p.QuantidadeEstoque < p.QuantidadeMinima)
            .ToListAsync(ct);

    public async Task<Peca?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default)
    {
        var query = tracking ? _db.Pecas : _db.Pecas.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<bool> ExistePorCodigoAsync(string codigo, int? ignorarId = null, CancellationToken ct = default) =>
        _db.Pecas.AnyAsync(p => p.Codigo == codigo && (ignorarId == null || p.Id != ignorarId), ct);

    public async Task AdicionarAsync(Peca peca, CancellationToken ct = default) =>
        await _db.Pecas.AddAsync(peca, ct);

    public void Remover(Peca peca) => _db.Pecas.Remove(peca);
}

public class ServicoRepository : IServicoRepository
{
    private readonly AppDbContext _db;
    public ServicoRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Servico>> ListarAsync(CancellationToken ct = default) =>
        await _db.Servicos.AsNoTracking().ToListAsync(ct);

    public async Task<Servico?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default)
    {
        var query = tracking ? _db.Servicos : _db.Servicos.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task AdicionarAsync(Servico servico, CancellationToken ct = default) =>
        await _db.Servicos.AddAsync(servico, ct);

    public void Remover(Servico servico) => _db.Servicos.Remove(servico);
}

public class OrdemServicoRepository : IOrdemServicoRepository
{
    private readonly AppDbContext _db;
    public OrdemServicoRepository(AppDbContext db) => _db = db;

    private IQueryable<OrdemServico> ComIncludes() =>
        _db.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Veiculo)
            .Include(o => o.Itens).ThenInclude(i => i.Servico)
            .Include(o => o.Itens).ThenInclude(i => i.Peca);

    public async Task<IReadOnlyList<OrdemServico>> ListarComIncludesAsync(CancellationToken ct = default) =>
        await ComIncludes().AsNoTracking().ToListAsync(ct);

    public async Task<OrdemServico?> ObterComIncludesAsync(int id, bool tracking = true, CancellationToken ct = default)
    {
        var query = tracking ? ComIncludes() : ComIncludes().AsNoTracking();
        return await query.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<OrdemServico?> ObterAsync(int id, bool tracking = false, CancellationToken ct = default)
    {
        var query = tracking ? _db.OrdensServico : _db.OrdensServico.AsNoTracking();
        return await query.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public Task<bool> ExistePorClienteAsync(int clienteId, CancellationToken ct = default) =>
        _db.OrdensServico.AnyAsync(o => o.ClienteId == clienteId, ct);

    public async Task<string?> UltimoNumeroOSAsync(string prefixo, CancellationToken ct = default) =>
        await _db.OrdensServico
            .Where(o => o.NumeroOS.StartsWith(prefixo))
            .OrderByDescending(o => o.NumeroOS)
            .Select(o => o.NumeroOS)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OrdemServico>> ListarFinalizadasComExecucaoAsync(CancellationToken ct = default) =>
        await _db.OrdensServico.AsNoTracking()
            .Where(o => o.DataInicioExecucao != null && o.DataFinalizacao != null)
            .ToListAsync(ct);

    public async Task AdicionarAsync(OrdemServico ordem, CancellationToken ct = default) =>
        await _db.OrdensServico.AddAsync(ordem, ct);

    public async Task AdicionarItemAsync(ItemOrdemServico item, CancellationToken ct = default) =>
        await _db.ItensOrdemServico.AddAsync(item, ct);

    public void RemoverItem(ItemOrdemServico item) => _db.ItensOrdemServico.Remove(item);
}
