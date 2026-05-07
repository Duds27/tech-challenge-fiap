using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using OficinaMecanicaBackend.Data;
using OficinaMecanicaBackend.Models;
using OficinaMecanicaBackend.Services;

namespace OficinaMecanicaBackend.Tests.Services;

public class PecaServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly PecaService _service;

    public PecaServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new PecaService(_db, NullLogger<PecaService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetEstoqueBaixoAsync_ReturnsOnlyBelowMinimum()
    {
        _db.Pecas.AddRange(
            new Peca { Nome = "Baixo",  PrecoUnitario = 10, QuantidadeEstoque = 2, QuantidadeMinima = 5, Ativo = true, DataCriacao = DateTime.UtcNow },
            new Peca { Nome = "Normal", PrecoUnitario = 10, QuantidadeEstoque = 10, QuantidadeMinima = 5, Ativo = true, DataCriacao = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetEstoqueBaixoAsync();

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("Baixo", result.Data!.First().Nome);
    }

    [Fact]
    public async Task DeductStockAsync_SubtractsQuantidadeCorretamente()
    {
        var peca = new Peca { Nome = "Filtro", PrecoUnitario = 50, QuantidadeEstoque = 10, QuantidadeMinima = 2, Ativo = true, DataCriacao = DateTime.UtcNow };
        _db.Pecas.Add(peca);
        await _db.SaveChangesAsync();

        var result = await _service.DeductStockAsync(peca.Id, 3);

        Assert.True(result.Success);
        await _db.Entry(peca).ReloadAsync();
        Assert.Equal(7, peca.QuantidadeEstoque);
    }

    [Fact]
    public async Task DeductStockAsync_EmiteLogWarning_QuandoAbaixoDoMinimo()
    {
        var peca = new Peca { Nome = "Vela", PrecoUnitario = 20, QuantidadeEstoque = 5, QuantidadeMinima = 5, Ativo = true, DataCriacao = DateTime.UtcNow };
        _db.Pecas.Add(peca);
        await _db.SaveChangesAsync();

        var logger = new TestLogger<PecaService>();
        var service = new PecaService(_db, logger);

        await service.DeductStockAsync(peca.Id, 1);

        Assert.Contains(logger.Logs, l => l.Level == LogLevel.Warning && l.Message.Contains("estoque mínimo"));
    }

    [Fact]
    public async Task DeductStockAsync_EstoqueInsuficiente_RetornaBadRequest()
    {
        var peca = new Peca { Nome = "Amortecedor", PrecoUnitario = 200, QuantidadeEstoque = 2, QuantidadeMinima = 1, Ativo = true, DataCriacao = DateTime.UtcNow };
        _db.Pecas.Add(peca);
        await _db.SaveChangesAsync();

        var result = await _service.DeductStockAsync(peca.Id, 5);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }
}
