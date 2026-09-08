using SchoolManagement.Application.DTOs.Schedules;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.DTOs.Students;

namespace SchoolManagement.Application.Interfaces;

public interface IStudentGroupService
{
    Task<IReadOnlyList<StudentGroupListItem>> ListAsync(
        StudentGroupFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentGroupOption>> ListOptionsAsync(
        int? academicLevelId = null,
        bool onlyActive = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentListItem>> ListStudentsAsync(
        int studentGroupId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassScheduleDto>> ListSchedulesAsync(
        int studentGroupId,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateStudentGroupRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateStudentGroupRequest request, CancellationToken cancellationToken = default);

    Task SetActiveAsync(int studentGroupId, bool isActive, CancellationToken cancellationToken = default);

    Task DeleteAsync(int studentGroupId, CancellationToken cancellationToken = default);
}
