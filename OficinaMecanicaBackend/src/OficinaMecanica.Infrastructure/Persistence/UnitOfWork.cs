using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Infrastructure.Data;

namespace OficinaMecanica.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> SalvarAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Traduz falhas de persistência (ex.: violação de unicidade em condição
            // de corrida) para uma exceção da camada de aplicação, mantendo os casos
            // de uso independentes do provedor EF/DB.
            throw new ConflitoPersistenciaException(
                "Falha ao persistir alterações no banco de dados.", ex);
        }
    }

    public async Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct = default)
    {
        var tx = await _db.Database.BeginTransactionAsync(ct);
        return new EfTransacao(tx);
    }

    private sealed class EfTransacao : ITransacao
    {
        private readonly IDbContextTransaction _tx;

        public EfTransacao(IDbContextTransaction tx) => _tx = tx;

        public Task CommitAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct = default) => _tx.RollbackAsync(ct);
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }
}
