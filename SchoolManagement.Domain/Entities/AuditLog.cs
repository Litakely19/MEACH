using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class AuditLog : BaseEntity
{
    /// <summary>Null for anonymous events such as a failed login attempt.</summary>
    public int? UserId { get; set; }

    public User? User { get; set; }

    public AuditAction Action { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Description { get; set; } = string.Empty;
}
