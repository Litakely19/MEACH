using SchoolManagement.Application.DTOs.Attendance;

namespace SchoolManagement.Application.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceSheet> GetSheetAsync(
        int studentGroupId,
        DateTime date,
        int? classScheduleId = null,
        CancellationToken cancellationToken = default);

    Task SaveAsync(SaveAttendanceRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceReportRow>> GetReportAsync(
        AttendanceReportFilter filter,
        CancellationToken cancellationToken = default);
}
