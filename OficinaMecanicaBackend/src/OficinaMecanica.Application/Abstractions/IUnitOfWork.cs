namespace OficinaMecanica.Application.Abstractions;

/// <summary>Coordena a persistência e o controle transacional dos repositórios.</summary>
public interface IUnitOfWork
{
    Task<int> SalvarAsync(CancellationToken ct = default);
    Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct = default);
}

/// <summary>Transação de unidade de trabalho; faça Commit ou Rollback e descarte.</summary>
public interface ITransacao : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
