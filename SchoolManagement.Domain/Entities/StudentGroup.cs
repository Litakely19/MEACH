namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Independent cohort inside an academic level, for example "L1 Group 09:00 - 10:00".
/// Students and attendance never mix across groups.
/// </summary>
public class StudentGroup : BaseEntity, ISoftDeletable
{
    public int AcademicLevelId { get; set; }

    public AcademicLevel AcademicLevel { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();

    public ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();
}
