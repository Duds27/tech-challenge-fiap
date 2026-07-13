using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.UseCases;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Infrastructure.Data;
using OficinaMecanica.Infrastructure.Persistence;

namespace OficinaMecanicaBackend.Tests.Services;

public class PecaUseCasesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly PecaUseCases _useCases;

    public PecaUseCasesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _useCases = new PecaUseCases(new PecaRepository(_db), new UnitOfWork(_db));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ListarEstoqueBaixoAsync_ReturnsOnlyBelowMinimum()
    {
        _db.Pecas.AddRange(
            new Peca { Nome = "Baixo",  PrecoUnitario = 10, QuantidadeEstoque = 2, QuantidadeMinima = 5, Ativo = true, DataCriacao = DateTime.UtcNow },
            new Peca { Nome = "Normal", PrecoUnitario = 10, QuantidadeEstoque = 10, QuantidadeMinima = 5, Ativo = true, DataCriacao = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var result = await _useCases.ListarEstoqueBaixoAsync();

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("Baixo", result.Data!.First().Nome);
    }

    [Fact]
    public async Task CriarAsync_PersisteEExpoeEstoqueBaixo()
    {
        var result = await _useCases.CriarAsync(new CreatePecaDto(
            "Pastilha", null, "PAST-1", PrecoUnitario: 80m,
            QuantidadeEstoque: 1, QuantidadeMinima: 5, Ativo: true));

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.True(result.Data!.EstoqueBaixo);
    }

    [Fact]
    public async Task CriarAsync_CodigoDuplicado_RetornaConflict()
    {
        await _useCases.CriarAsync(new CreatePecaDto("A", null, "DUP", 10m, 5, 1, true));

        var result = await _useCases.CriarAsync(new CreatePecaDto("B", null, "DUP", 10m, 5, 1, true));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task ExcluirAsync_Inexistente_RetornaNotFound()
    {
        var result = await _useCases.ExcluirAsync(9999);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }
}
