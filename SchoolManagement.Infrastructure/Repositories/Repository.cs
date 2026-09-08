using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    public Repository(SchoolDbContext context)
    {
        Context = context;
    }

    protected SchoolDbContext Context { get; }

    protected DbSet<T> Set => Context.Set<T>();

    public virtual Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Query(trackChanges: true).FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Query().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(entities, cancellationToken);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);

    public IQueryable<T> Query(bool trackChanges = false)
    {
        var query = trackChanges ? Set.AsQueryable() : Set.AsNoTracking();
        return ApplyDefaultFilters(query);
    }

    /// <summary>
    /// Hook used by soft deletable repositories to hide removed rows from every read.
    /// </summary>
    protected virtual IQueryable<T> ApplyDefaultFilters(IQueryable<T> query) => query;
}
