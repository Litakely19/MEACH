using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<Attendance?> GetAsync(
        int studentId,
        int classScheduleId,
        DateTime attendanceDate,
        CancellationToken cancellationToken = default);
}
