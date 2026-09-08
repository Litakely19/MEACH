namespace SchoolManagement.Application.DTOs.StudentGroups;

public record StudentGroupListItem(
    int Id,
    string Name,
    string? Description,
    int AcademicLevelId,
    string AcademicLevelName,
    bool IsActive,
    int StudentCount,
    int ScheduleCount);

public record StudentGroupOption(int Id, string Name, int AcademicLevelId, string AcademicLevelName, bool IsActive)
{
    public string DisplayName => $"{AcademicLevelName} — {Name}";
}

public record StudentGroupFilter(
    string? SearchTerm = null,
    int? AcademicLevelId = null,
    bool? IsActive = null);

public record CreateStudentGroupRequest(int AcademicLevelId, string Name, string? Description);

public record UpdateStudentGroupRequest(int Id, int AcademicLevelId, string Name, string? Description, bool IsActive);
