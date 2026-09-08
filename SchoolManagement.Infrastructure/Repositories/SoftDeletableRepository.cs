using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

/// <summary>
/// Repository for entities that are hidden rather than erased. Reads skip removed
/// rows and <see cref="Remove"/> flags the row instead of issuing a DELETE, so
/// invoices, payments and receipts keep resolving their student or class.
/// </summary>
public class SoftDeletableRepository<T> : Repository<T> where T : BaseEntity, ISoftDeletable
{
    public SoftDeletableRepository(SchoolDbContext context) : base(context)
    {
    }

    public override void Remove(T entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        Set.Update(entity);
    }

    /// <summary>Read access that also returns removed rows, for audit screens and restores.</summary>
    public IQueryable<T> QueryIncludingDeleted(bool trackChanges = false) =>
        trackChanges ? Set.AsQueryable() : Set.AsNoTracking();

    protected override IQueryable<T> ApplyDefaultFilters(IQueryable<T> query) =>
        query.Where(entity => !entity.IsDeleted);
}
