using SchoolManagement.Application.DTOs.Schedules;

namespace SchoolManagement.Application.Interfaces;

public interface IClassScheduleService
{
    Task<IReadOnlyList<ClassScheduleDto>> ListByGroupAsync(
        int studentGroupId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassScheduleDto>> ListByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateClassScheduleRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateClassScheduleRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int scheduleId, CancellationToken cancellationToken = default);
}
