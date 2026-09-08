using SchoolManagement.Application.DTOs.AcademicLevels;
using SchoolManagement.Application.DTOs.StudentGroups;

namespace SchoolManagement.Application.Interfaces;

public interface IAcademicLevelService
{
    Task<IReadOnlyList<AcademicLevelListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicLevelOption>> ListOptionsAsync(
        bool onlyActive = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentGroupListItem>> ListGroupsAsync(
        int academicLevelId,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateAcademicLevelRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateAcademicLevelRequest request, CancellationToken cancellationToken = default);

    Task SetActiveAsync(int academicLevelId, bool isActive, CancellationToken cancellationToken = default);
}
