using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

/// <summary>
/// Generic persistence contract. Write operations stage changes only; the owning
/// <see cref="IUnitOfWork"/> decides when they are committed.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    /// <summary>
    /// Composable read access used by services for filtered lists and reports.
    /// Tracking is disabled by default because query results feed read models.
    /// </summary>
    IQueryable<T> Query(bool trackChanges = false);
}
