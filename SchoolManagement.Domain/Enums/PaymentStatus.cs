namespace SchoolManagement.Domain.Enums;

/// <summary>
/// Payments are never physically removed; they move through these states instead.
/// </summary>
public enum PaymentStatus
{
    Active = 1,
    Cancelled = 2,
    Reversed = 3
}
