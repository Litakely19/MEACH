using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

/// <summary>
/// One financial obligation for one student: a monthly Écolage line or a one-time
/// fee such as Droit, Livre or an examination. Balances are stored on this row
/// and updated inside the payment transaction.
/// </summary>
public class StudentFee : BaseEntity
{
    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int PaymentTypeId { get; set; }

    public PaymentType PaymentType { get; set; } = null!;

    public decimal ExpectedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public DateTime DueDate { get; set; }

    /// <summary>1-12 for monthly Écolage; null for one-time fees.</summary>
    public int? Month { get; set; }

    public int? Year { get; set; }

    /// <summary>School year for one-time fees (Droit, Livre, exams). Null for monthly lines.</summary>
    public int? SchoolYearId { get; set; }

    public SchoolYear? SchoolYear { get; set; }

    public FeeStatus Status { get; set; } = FeeStatus.Unpaid;

    public bool IsMandatory { get; set; } = true;

    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public decimal RemainingAmount => ExpectedAmount - PaidAmount;
}
