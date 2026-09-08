using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<Attendance?> GetAsync(
        int studentId,
        int classScheduleId,
        DateTime attendanceDate,
        CancellationToken cancellationToken = default)
    {
        var date = attendanceDate.Date;
        return Query(trackChanges: true).FirstOrDefaultAsync(
            attendance => attendance.StudentId == studentId
                && attendance.ClassScheduleId == classScheduleId
                && attendance.AttendanceDate == date,
            cancellationToken);
    }
}
