namespace SchoolManagement.Application.DTOs.SchoolYears;

public record SchoolYearListItem(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    bool IsCurrent,
    bool IsClosed,
    DateTime? ClosedAt,
    int ClassCount,
    int StudentCount);

public record CreateSchoolYearRequest(string Name, DateTime StartDate, DateTime EndDate, bool SetAsCurrent);

public record UpdateSchoolYearRequest(int Id, string Name, DateTime StartDate, DateTime EndDate, bool SetAsCurrent);

public record SchoolYearOption(int Id, string Name, bool IsCurrent, bool IsClosed, DateTime StartDate, DateTime EndDate)
{
    public string DisplayName => IsCurrent ? $"{Name} (current)" : Name;
}
