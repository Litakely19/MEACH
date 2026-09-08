namespace SchoolManagement.Domain.Entities;

public class SchoolYear : BaseEntity
{
    /// <summary>Display label such as "2026-2027".</summary>
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsCurrent { get; set; }

    /// <summary>
    /// A closed year rejects new financial transactions until an administrator reopens it.
    /// </summary>
    public bool IsClosed { get; set; }

    public DateTime? ClosedAt { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();

    public ICollection<StudentFee> StudentFees { get; set; } = new List<StudentFee>();
}
