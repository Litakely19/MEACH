using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface ISchoolYearRepository : IRepository<SchoolYear>
{
    Task<SchoolYear?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    Task ClearCurrentFlagAsync(int exceptSchoolYearId, CancellationToken cancellationToken = default);
}
