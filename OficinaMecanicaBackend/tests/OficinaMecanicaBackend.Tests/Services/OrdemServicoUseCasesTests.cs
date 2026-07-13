using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OficinaMecanica.Application.DTOs.OrdensServico;
using OficinaMecanica.Application.UseCases;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Infrastructure.Data;
using OficinaMecanica.Infrastructure.Persistence;

namespace OficinaMecanicaBackend.Tests.Services;

public class OrdemServicoUseCasesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly FakeNotificador _notificador = new();
    private readonly OrdemServicoUseCases _useCases;

    public OrdemServicoUseCasesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _useCases = BuildUseCases(NullLogger<OrdemServicoUseCases>.Instance);
    }

    private OrdemServicoUseCases BuildUseCases(ILogger<OrdemServicoUseCases> logger) => new(
        new OrdemServicoRepository(_db),
        new ClienteRepository(_db),
        new VeiculoRepository(_db),
        new PecaRepository(_db),
        new ServicoRepository(_db),
        new UnitOfWork(_db),
        _notificador,
        logger);

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ────── helpers ──────

    private async Task<(Cliente cliente, Veiculo veiculo)> SeedClienteVeiculoAsync()
    {
        var cliente = new Cliente { CpfCnpj = "52998224725", Nome = "Teste", Email = "cli@ex.com", DataCriacao = DateTime.UtcNow };
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

    private async Task<OrdemServico> SeedOrdemFinalizadaAsync(int clienteId, int veiculoId,
        DateTime dataInicioExecucao, DateTime dataFinalizacao)
    {
        var os = new OrdemServico
        {
            NumeroOS = $"OS-{DateTime.UtcNow.Year}-{Random.Shared.Next(100000, 999999):D6}",
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            Status = StatusOrdemServico.Finalizada,
            ValorTotal = 0,
            DataCriacao = dataInicioExecucao.AddHours(-1),
            DataInicioExecucao = dataInicioExecucao,
            DataFinalizacao = dataFinalizacao
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

    private async Task<Peca> SeedPecaAsync(decimal preco = 50m, int estoque = 10, int minima = 1)
    {
        var p = new Peca { Nome = "Filtro", PrecoUnitario = preco, QuantidadeEstoque = estoque, QuantidadeMinima = minima, Ativo = true, DataCriacao = DateTime.UtcNow };
        _db.Pecas.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    // ────── CriarAsync ──────

    [Fact]
    public async Task CriarAsync_GeraNumeroOSNoFormatoCorreto()
    {
        var (c, v) = await SeedClienteVeiculoAsync();

        var result = await _useCases.CriarAsync(new CreateOrdemServicoDto(c.Id, v.Id, null, null));

        Assert.True(result.Success);
        Assert.Matches($@"^OS-{DateTime.UtcNow.Year}-\d{{6}}$", result.Data!.NumeroOS);
    }

    // ────── AvancarStatusAsync ──────

    [Fact]
    public async Task AvancarStatusAsync_TransicaoValida_RetornaSucessoENotifica()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);

        var result = await _useCases.AvancarStatusAsync(os.Id,
            new UpdateStatusDto(StatusOrdemServico.EmDiagnostico));

        Assert.True(result.Success);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, result.Data!.Status);
        Assert.Contains(_notificador.Notificacoes,
            n => n.Anterior == StatusOrdemServico.Recebida && n.Novo == StatusOrdemServico.EmDiagnostico);
    }

    [Fact]
    public async Task AvancarStatusAsync_EmExecucao_DefineDataInicioExecucao()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.AguardandoAprovacao, orcamentoAprovado: true);

        var result = await _useCases.AvancarStatusAsync(os.Id,
            new UpdateStatusDto(StatusOrdemServico.EmExecucao));

        Assert.True(result.Success);
        Assert.Equal(StatusOrdemServico.EmExecucao, result.Data!.Status);
        Assert.NotNull(result.Data.DataInicioExecucao);
    }

    [Fact]
    public async Task AvancarStatusAsync_TransicaoInvalida_RetornaBadRequest()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);

        var result = await _useCases.AvancarStatusAsync(os.Id,
            new UpdateStatusDto(StatusOrdemServico.EmExecucao));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task AvancarStatusAsync_AguardandoAprovacaoSemAprovacao_RetornaBadRequest()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.AguardandoAprovacao, orcamentoAprovado: false);

        var result = await _useCases.AvancarStatusAsync(os.Id,
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

        var result = await _useCases.AprovarOrcamentoAsync(os.Id, new AprovarOrcamentoDto(true));

        Assert.True(result.Success);
        Assert.True(result.Data!.OrcamentoAprovado);
        Assert.NotNull(result.Data.DataAprovacaoOrcamento);
    }

    [Fact]
    public async Task AprovarOrcamentoAsync_StatusIncorreto_RetornaErro()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.Recebida);

        var result = await _useCases.AprovarOrcamentoAsync(os.Id, new AprovarOrcamentoDto(true));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    // ────── AdicionarItemAsync ──────

    [Fact]
    public async Task AdicionarItemAsync_PecaEstoqueDebitadoCorretamente()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var peca = await SeedPecaAsync(estoque: 10);

        await _useCases.AdicionarItemAsync(os.Id, new AddItemDto(TipoItemOrdemServico.Peca, null, peca.Id, 3));

        await _db.Entry(peca).ReloadAsync();
        Assert.Equal(7, peca.QuantidadeEstoque);
    }

    [Fact]
    public async Task AdicionarItemAsync_EstoqueInsuficiente_RetornaBadRequest()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var peca = await SeedPecaAsync(estoque: 2);

        var result = await _useCases.AdicionarItemAsync(os.Id,
            new AddItemDto(TipoItemOrdemServico.Peca, null, peca.Id, 5));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task AdicionarItemAsync_ValorTotalRecalculadoCorretamente()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var servico = await SeedServicoAsync(preco: 100m);

        var result = await _useCases.AdicionarItemAsync(os.Id,
            new AddItemDto(TipoItemOrdemServico.Servico, servico.Id, null, 2));

        Assert.True(result.Success);
        Assert.Equal(200m, result.Data!.ValorTotal);
    }

    [Fact]
    public async Task AdicionarItemAsync_EstoqueAbaixoDoMinimo_EmiteLogWarning()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var peca = await SeedPecaAsync(estoque: 5, minima: 5);

        var logger = new TestLogger<OrdemServicoUseCases>();
        var useCases = BuildUseCases(logger);

        await useCases.AdicionarItemAsync(os.Id, new AddItemDto(TipoItemOrdemServico.Peca, null, peca.Id, 1));

        Assert.Contains(logger.Logs, l => l.Level == LogLevel.Warning && l.Message.Contains("estoque mínimo"));
    }

    // ────── RemoverItemAsync ──────

    [Fact]
    public async Task RemoverItemAsync_EstoqueRestauradoAoRemoverItemDePeca()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var os = await SeedOrdemAsync(c.Id, v.Id);
        var peca = await SeedPecaAsync(estoque: 10);

        await _useCases.AdicionarItemAsync(os.Id, new AddItemDto(TipoItemOrdemServico.Peca, null, peca.Id, 4));
        await _db.Entry(peca).ReloadAsync();
        Assert.Equal(6, peca.QuantidadeEstoque);

        var osComItens = await _useCases.ObterAsync(os.Id);
        var itemId = osComItens.Data!.Itens.First().Id;

        await _useCases.RemoverItemAsync(os.Id, itemId);
        await _db.Entry(peca).ReloadAsync();
        Assert.Equal(10, peca.QuantidadeEstoque);
    }

    // ────── ListarAsync (ordenação + exclusão lógica) ──────

    [Fact]
    public async Task ListarAsync_OrdenaPorPrioridadeEExcluiConcluidas()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.Recebida);
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.EmExecucao);
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.AguardandoAprovacao);
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.Finalizada);
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.Entregue);

        var result = await _useCases.ListarAsync();

        Assert.True(result.Success);
        var lista = result.Data!.ToList();
        // Finalizada e Entregue são omitidas
        Assert.Equal(3, lista.Count);
        Assert.DoesNotContain(lista, o => o.Status is StatusOrdemServico.Finalizada or StatusOrdemServico.Entregue);
        // Ordem: EmExecucao > AguardandoAprovacao > Recebida
        Assert.Equal(StatusOrdemServico.EmExecucao, lista[0].Status);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, lista[1].Status);
        Assert.Equal(StatusOrdemServico.Recebida, lista[2].Status);
    }

    [Fact]
    public async Task ListarAsync_IncluirConcluidas_RetornaTodas()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.Recebida);
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.Finalizada);

        var result = await _useCases.ListarAsync(incluirConcluidas: true);

        Assert.Equal(2, result.Data!.Count());
    }

    // ────── CalcularTempoMedioExecucaoAsync ──────

    [Fact]
    public async Task CalcularTempoMedioExecucaoAsync_SemOrdens_RetornaZero()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.EmExecucao);

        var result = await _useCases.CalcularTempoMedioExecucaoAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.Data!.OrdensConsideradas);
        Assert.Equal("00:00:00", result.Data.TempoMedioFormatado);
    }

    [Fact]
    public async Task CalcularTempoMedioExecucaoAsync_CalculaMediaDasFinalizadas()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var baseData = DateTime.UtcNow;
        await SeedOrdemFinalizadaAsync(c.Id, v.Id, baseData, baseData.AddHours(2));
        await SeedOrdemFinalizadaAsync(c.Id, v.Id, baseData, baseData.AddHours(4));

        var result = await _useCases.CalcularTempoMedioExecucaoAsync();

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.OrdensConsideradas);
        Assert.Equal(10800, result.Data.TempoMedioSegundos);
        Assert.Equal(3, result.Data.TempoMedioHoras);
        Assert.Equal("03:00:00", result.Data.TempoMedioFormatado);
    }

    [Fact]
    public async Task CalcularTempoMedioExecucaoAsync_IgnoraNaoFinalizadas()
    {
        var (c, v) = await SeedClienteVeiculoAsync();
        var baseData = DateTime.UtcNow;
        await SeedOrdemFinalizadaAsync(c.Id, v.Id, baseData, baseData.AddHours(1));
        await SeedOrdemAsync(c.Id, v.Id, StatusOrdemServico.EmExecucao);

        var result = await _useCases.CalcularTempoMedioExecucaoAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.Data!.OrdensConsideradas);
        Assert.Equal(3600, result.Data.TempoMedioSegundos);
    }
}
