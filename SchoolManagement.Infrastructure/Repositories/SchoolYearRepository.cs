using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class SchoolYearRepository : Repository<SchoolYear>, ISchoolYearRepository
{
    public SchoolYearRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<SchoolYear?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(year => year.IsCurrent, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
        Query().AnyAsync(
            year => year.Name == name && (excludeId == null || year.Id != excludeId),
            cancellationToken);

    public async Task ClearCurrentFlagAsync(int exceptSchoolYearId, CancellationToken cancellationToken = default)
    {
        var others = await Query(trackChanges: true)
            .Where(year => year.IsCurrent && year.Id != exceptSchoolYearId)
            .ToListAsync(cancellationToken);

        foreach (var year in others)
        {
            year.IsCurrent = false;
        }
    }
}
