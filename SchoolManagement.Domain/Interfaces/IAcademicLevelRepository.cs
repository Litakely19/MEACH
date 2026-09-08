using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IAcademicLevelRepository : IRepository<AcademicLevel>
{
    Task<AcademicLevel?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
}
