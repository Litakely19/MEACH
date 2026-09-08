using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class AcademicLevelRepository : SoftDeletableRepository<AcademicLevel>, IAcademicLevelRepository
{
    public AcademicLevelRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<AcademicLevel?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Query(trackChanges: true).FirstOrDefaultAsync(level => level.Name == name, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = Query().Where(level => level.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(level => level.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
