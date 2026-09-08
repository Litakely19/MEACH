using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class Student : BaseEntity, ISoftDeletable
{
    /// <summary>Automatically generated, unique identifier such as "STD-2026-0001".</summary>
    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    public DateTime DateOfBirth { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public int AcademicLevelId { get; set; }

    public AcademicLevel AcademicLevel { get; set; } = null!;

    public int StudentGroupId { get; set; }

    public StudentGroup StudentGroup { get; set; } = null!;

    public int? SchoolYearId { get; set; }

    public SchoolYear? SchoolYear { get; set; }

    public DateTime EnrollmentDate { get; set; }

    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<StudentFee> Fees { get; set; } = new List<StudentFee>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
