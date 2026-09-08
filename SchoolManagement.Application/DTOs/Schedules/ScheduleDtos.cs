namespace SchoolManagement.Application.DTOs.Schedules;

public record ClassScheduleDto(
    int Id,
    int StudentGroupId,
    string StudentGroupName,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string SessionLabel);

public record CreateClassScheduleRequest(
    int StudentGroupId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime);

public record UpdateClassScheduleRequest(
    int Id,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime);
