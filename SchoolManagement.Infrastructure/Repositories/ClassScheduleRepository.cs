using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class ClassScheduleRepository : Repository<ClassSchedule>, IClassScheduleRepository
{
    public ClassScheduleRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<bool> OverlapsAsync(
        int studentGroupId,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(schedule =>
            schedule.StudentGroupId == studentGroupId
            && schedule.DayOfWeek == dayOfWeek
            && schedule.StartTime < endTime
            && startTime < schedule.EndTime);

        if (excludeId.HasValue)
        {
            query = query.Where(schedule => schedule.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
