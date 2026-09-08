using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class Attendance : BaseEntity
{
    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int ClassScheduleId { get; set; }

    public ClassSchedule ClassSchedule { get; set; } = null!;

    public DateTime AttendanceDate { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Remarks { get; set; }

    public int RecordedByUserId { get; set; }

    public User RecordedByUser { get; set; } = null!;
}
