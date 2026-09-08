namespace SchoolManagement.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Self describing PBKDF2 hash (algorithm, iteration count, salt and key), never a plain password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set for seeded or reset accounts; the shell forces a password change before granting access.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Payment> ReceivedPayments { get; set; } = new List<Payment>();

    public ICollection<StudentFee> CreatedFees { get; set; } = new List<StudentFee>();

    public ICollection<Attendance> RecordedAttendances { get; set; } = new List<Attendance>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
