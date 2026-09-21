using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Payments;

public record PaymentListItem(
    int Id,
    string PaymentNumber,
    int StudentId,
    string StudentNumber,
    string FirstName,
    string LastName,
    string AcademicLevelName,
    string StudentGroupName,
    string PaymentTypeName,
    string PeriodLabel,
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    string? Reference,
    string ReceivedBy,
    PaymentStatus Status,
    string? ReceiptNumber)
{
    public string StudentName => $"{FirstName} {LastName}".Trim();
}

public record PaymentFilter(
    string? SearchTerm = null,
    int? StudentId = null,
    int? AcademicLevelId = null,
    int? StudentGroupId = null,
    int? PaymentTypeId = null,
    PaymentMethod? PaymentMethod = null,
    PaymentStatus? Status = null,
    int? ReceivedByUserId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 25);

public record RegisterPaymentRequest(
    int StudentId,
    int StudentFeeId,
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    string? Reference,
    string? Notes,
    bool DuplicateConfirmed = false);

/// <summary>Settles several obligations for one student in a single register operation.</summary>
public record RegisterMultiPaymentRequest(
    int StudentId,
    IReadOnlyList<PaymentAllocationLine> Allocations,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    string? Reference,
    string? Notes,
    bool DuplicateConfirmed = false);

public record PaymentAllocationLine(int StudentFeeId, decimal Amount);

public record RegisterMultiPaymentResult(
    IReadOnlyList<RegisterPaymentResult> Payments,
    decimal TotalApplied,
    int? PrimaryReceiptId,
    string? PrimaryReceiptNumber);

public record RegisterPaymentResult(
    int PaymentId,
    string PaymentNumber,
    int ReceiptId,
    string ReceiptNumber,
    decimal AmountApplied,
    decimal FeeRemainingAmount);

public record DuplicatePaymentWarning(
    int ExistingPaymentId,
    string ExistingPaymentNumber,
    decimal Amount,
    DateTime PaymentDate,
    string ReceivedBy);

public record PaymentDetail(
    int Id,
    string PaymentNumber,
    int StudentId,
    string StudentNumber,
    string StudentName,
    string AcademicLevelName,
    string StudentGroupName,
    int StudentFeeId,
    string PaymentTypeName,
    string PeriodLabel,
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    string? Reference,
    string? Notes,
    string ReceivedBy,
    PaymentStatus Status,
    DateTime? CancelledAt,
    string? CancelledBy,
    string? CancellationReason,
    int? ReceiptId,
    string? ReceiptNumber);

public record CancelPaymentRequest(int PaymentId, string Reason, bool Reverse = false);
