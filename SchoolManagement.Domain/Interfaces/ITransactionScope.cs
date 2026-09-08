namespace SchoolManagement.Domain.Interfaces;

/// <summary>
/// Explicit database transaction. Disposing without committing rolls back, which
/// guarantees that a failed payment never leaves an invoice balance half updated.
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
