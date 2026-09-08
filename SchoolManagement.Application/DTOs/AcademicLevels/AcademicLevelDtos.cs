namespace SchoolManagement.Application.DTOs.AcademicLevels;

public record AcademicLevelListItem(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int GroupCount,
    int StudentCount);

public record AcademicLevelOption(int Id, string Name, bool IsActive)
{
    public string DisplayName => IsActive ? Name : $"{Name} (inactive)";
}

public record CreateAcademicLevelRequest(string Name, string? Description);

public record UpdateAcademicLevelRequest(int Id, string Name, string? Description, bool IsActive);
