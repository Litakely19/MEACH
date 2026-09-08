namespace SchoolManagement.Domain.Enums;

/// <summary>
/// Settlement state of an individual student fee.
/// </summary>
public enum FeeStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}
