using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IStudentGroupRepository : IRepository<StudentGroup>
{
    Task<StudentGroup?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int academicLevelId,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountStudentsAsync(int studentGroupId, CancellationToken cancellationToken = default);
}
