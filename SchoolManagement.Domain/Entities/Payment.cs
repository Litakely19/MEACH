using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class Payment : BaseEntity
{
    /// <summary>Unique, automatically generated number such as "PAY-2026-0001".</summary>
    public string PaymentNumber { get; set; } = string.Empty;

    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int StudentFeeId { get; set; }

    public StudentFee StudentFee { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>External reference: cheque number, mobile money transaction id, ...</summary>
    public string? Reference { get; set; }

    public int ReceivedByUserId { get; set; }

    public User ReceivedByUser { get; set; } = null!;

    public PaymentStatus Status { get; set; } = PaymentStatus.Active;

    public string? Notes { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int? CancelledByUserId { get; set; }

    public User? CancelledByUser { get; set; }

    public string? CancellationReason { get; set; }

    public Receipt? Receipt { get; set; }
}
