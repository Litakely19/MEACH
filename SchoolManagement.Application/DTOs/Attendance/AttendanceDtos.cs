using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Attendance;

public record AttendanceSheetStudent(
    int StudentId,
    string StudentNumber,
    string StudentName,
    int? AttendanceId,
    AttendanceStatus Status,
    string? Remarks);

public record AttendanceSheet(
    int AcademicLevelId,
    string AcademicLevelName,
    int StudentGroupId,
    string StudentGroupName,
    DateTime Date,
    ClassScheduleSession? Session,
    IReadOnlyList<AttendanceSheetStudent> Students);

public record ClassScheduleSession(
    int ClassScheduleId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string SessionLabel);

public record AttendanceEntry(
    int StudentId,
    AttendanceStatus Status,
    string? Remarks);

public record SaveAttendanceRequest(
    int StudentGroupId,
    int ClassScheduleId,
    DateTime Date,
    IReadOnlyList<AttendanceEntry> Entries);

public record AttendanceReportRow(
    int StudentId,
    string StudentNumber,
    string StudentName,
    string AcademicLevelName,
    string StudentGroupName,
    int TotalSessions,
    int Present,
    int Absent,
    int Late,
    int Excused,
    decimal AttendancePercentage);

public record AttendanceReportFilter(
    int? StudentId = null,
    int? AcademicLevelId = null,
    int? StudentGroupId = null,
    DateTime? From = null,
    DateTime? To = null,
    int? Month = null,
    int? Year = null);
