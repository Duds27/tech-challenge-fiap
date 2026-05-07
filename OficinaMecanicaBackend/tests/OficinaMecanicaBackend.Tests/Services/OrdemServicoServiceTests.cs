using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OficinaMecanicaBackend.Data;
using OficinaMecanicaBackend.DTOs.OrdensServico;
using OficinaMecanicaBackend.Models;
using OficinaMecanicaBackend.Models.Enums;
using OficinaMecanicaBackend.Services;

namespace OficinaMecanicaBackend.Tests.Services;

public class OrdemServicoServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly OrdemServicoService _service;

    public OrdemServicoServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new OrdemServicoService(_db, NullLogger<OrdemServicoService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ────── helpers ──────

    private async Task<(Cliente cliente, Veiculo veiculo)> SeedClienteVeiculoAsync()
    {
        var cliente = new Cliente { CpfCnpj = "52998224725", Nome = "Teste", DataCriacao = DateTime.UtcNow };
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        var veiculo = new Veiculo { Placa = "ABC1234", Marca = "Ford", Modelo = "Ka", Ano = 2020, ClienteId = cliente.Id, DataCriacao = DateTime.UtcNow };
        _db.Veiculos.Add(veiculo);
        await _db.SaveChangesAsync();

        return (cliente, veiculo);
    }

    private async Task<OrdemServico> SeedOrdemAsync(int clienteId, int veiculoId,
        StatusOrdemServico status = StatusOrdemServico.Recebida, bool orcamentoAprovado = false)
    {
        var os = new OrdemServico
        {
            NumeroOS = $"OS-{DateTime.UtcNow.Year}-{Random.Shared.Next(100000, 999999):D6}",
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            Status = status,
            OrcamentoAprovado = orcamentoAprovado,
            ValorTotal = 0,
            DataCriacao = DateTime.UtcNow
        };
        _db.OrdensServico.Add(os);
        await _db.SaveChangesAsync();
        return os;
    }

    private async Task<Servico> SeedServicoAsync(decimal preco = 100m)
    {
        var s = new Servico { Nome = "Troca de óleo", PrecoBase = preco, Ativo = true, DataCriacao = DateTime.UtcNow };
        _db.Servicos.Add(s);
        await _db.SaveChangesAsync();
        return s;
    }

    private async Task<Peca> SeedPecaAsync(decimal preco = 50m, int estoque = 10)
    {
        var p = new Peca { Nome = "Filtro", PrecoUnitario = preco, QuantidadeEstoque = estoque, QuantidadeMinima = 1, Ativo = true, DataCriacao = DateTime.UtcNow };
        _db.Pecas.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    // ────── CreateAsync ──────

    [Fact]
    public async Task CreateAsync_GeraNumeroOSNoFormatoCorreto()
    {
        var (c, v) = await SeedClienteVeiculoAsync();

        var result = await _service.CreateAsync(new CreateOrdemServicoDto(c.Id, v.Id, null, null));

        Assert.True(result.Success);
        Assert.Matches($@"^OS-{DateTime.UtcNow.Year}-\d{{6}}$", result.Data!.NumeroOS);
    }

    // ────── AdvanceStatusAsync ──────

    [Fact]
    public async Task AdvanceStatusAsync_TransicaoValida_RetornaSucesso()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);

        var result = await _service.AdvanceStatusAsync(os.Id,
            new UpdateStatusDto(StatusOrdemServico.EmDiagnostico));

        Assert.True(result.Success);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, result.Data!.Status);
    }

    [Fact]
    public async Task AdvanceStatusAsync_TransicaoInvalida_RetornaBadRequest()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);

        // Recebida → EmExecucao é inválido
        var result = await _service.AdvanceStatusAsync(os.Id,
            new UpdateStatusDto(StatusOrdemServico.EmExecucao));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task AdvanceStatusAsync_AguardandoAprovacaoSemAprovacao_RetornaBadRequest()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.AguardandoAprovacao, orcamentoAprovado: false);

        var result = await _service.AdvanceStatusAsync(os.Id,
            new UpdateStatusDto(StatusOrdemServico.EmExecucao));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    // ────── AprovarOrcamentoAsync ──────

    [Fact]
    public async Task AprovarOrcamentoAsync_DefineFlagEDataCorretamente()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.AguardandoAprovacao);

        var result = await _service.AprovarOrcamentoAsync(os.Id, new AprovarOrcamentoDto(true));

        Assert.True(result.Success);
        Assert.True(result.Data!.OrcamentoAprovado);
        Assert.NotNull(result.Data.DataAprovacaoOrcamento);
    }

    [Fact]
    public async Task AprovarOrcamentoAsync_StatusIncorreto_RetornaErro()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.Recebida);

        var result = await _service.AprovarOrcamentoAsync(os.Id, new AprovarOrcamentoDto(true));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    // ────── AddItemAsync ──────

    [Fact]
    public async Task AddItemAsync_PecaEstoqueDebitadoCorretamente()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var peca = await SeedPecaAsync(estoque: 10);

        await _service.AddItemAsync(os.Id, new AddItemDto(TipoItemOrdemServico.Peca, null, peca.Id, 3));

        await _db.Entry(peca).ReloadAsync();
        Assert.Equal(7, peca.QuantidadeEstoque);
    }

    [Fact]
    public async Task AddItemAsync_EstoqueInsuficiente_RetornaBadRequest()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var peca = await SeedPecaAsync(estoque: 2);

        var result = await _service.AddItemAsync(os.Id,
            new AddItemDto(TipoItemOrdemServico.Peca, null, peca.Id, 5));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task AddItemAsync_ValorTotalRecalculadoCorretamente()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var servico = await SeedServicoAsync(preco: 100m);

        var result = await _service.AddItemAsync(os.Id,
            new AddItemDto(TipoItemOrdemServico.Servico, servico.Id, null, 2));

        Assert.True(result.Success);
        Assert.Equal(200m, result.Data!.ValorTotal);
    }

    // ────── DeleteItemAsync ──────

    [Fact]
    public async Task DeleteItemAsync_EstoqueRestauradoAoRemoverItemDePeca()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var peca = await SeedPecaAsync(estoque: 10);

        await _service.AddItemAsync(os.Id, new AddItemDto(TipoItemOrdemServico.Peca, null, peca.Id, 4));
        await _db.Entry(peca).ReloadAsync();
        Assert.Equal(6, peca.QuantidadeEstoque);

        var osComItens = await _service.GetByIdAsync(os.Id);
        var itemId = osComItens.Data!.Itens.First().Id;

        await _service.DeleteItemAsync(os.Id, itemId);
        await _db.Entry(peca).ReloadAsync();
        Assert.Equal(10, peca.QuantidadeEstoque);
    }
}
