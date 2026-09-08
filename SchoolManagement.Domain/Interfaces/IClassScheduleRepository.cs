using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IClassScheduleRepository : IRepository<ClassSchedule>
{
    Task<bool> OverlapsAsync(
        int studentGroupId,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime,
        int? excludeId = null,
        CancellationToken cancellationToken = default);
}
