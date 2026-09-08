namespace SchoolManagement.Domain.Entities;

public class ClassSchedule : BaseEntity
{
    public int StudentGroupId { get; set; }

    public StudentGroup StudentGroup { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public string SessionLabel => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
}
