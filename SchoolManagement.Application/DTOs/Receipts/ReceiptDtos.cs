using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Receipts;

public record ReceiptListItem(
    int Id,
    string ReceiptNumber,
    int PaymentId,
    string PaymentNumber,
    string StudentNumber,
    string StudentName,
    string AcademicLevelName,
    string StudentGroupName,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime IssueDate,
    string IssuedBy,
    int PrintCount,
    PaymentStatus PaymentStatus);

public record ReceiptFilter(
    string? SearchTerm = null,
    int? StudentId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 25);

public record ReceiptLine(string PaymentTypeName, string Description, string PeriodLabel, decimal Amount);

public record ReceiptDocument(
    string SchoolName,
    string? SchoolAddress,
    string? SchoolPhone,
    string? SchoolEmail,
    string? LogoPath,
    string ReceiptNumber,
    DateTime IssueDate,
    string PaymentNumber,
    string StudentName,
    string StudentNumber,
    string AcademicLevelName,
    string StudentGroupName,
    IReadOnlyList<ReceiptLine> Lines,
    decimal AmountPaid,
    PaymentMethod PaymentMethod,
    string? Reference,
    DateTime PaymentDate,
    string ReceivedBy,
    decimal ObligationTotal,
    decimal ObligationPaid,
    decimal ObligationRemaining,
    string CurrencySymbol,
    string? Footer,
    bool IsReprint,
    int PrintCount,
    PaymentStatus PaymentStatus);
