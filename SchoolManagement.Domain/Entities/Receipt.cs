namespace SchoolManagement.Domain.Entities;

public class Receipt : BaseEntity
{
    /// <summary>Unique, automatically generated number such as "REC-2026-0001".</summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    public int PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;

    public DateTime IssueDate { get; set; }

    public int IssuedByUserId { get; set; }

    public User IssuedByUser { get; set; } = null!;

    public int PrintCount { get; set; }

    public DateTime? LastPrintedAt { get; set; }
}
