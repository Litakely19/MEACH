using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Fees;

public record StudentFeeItem(
    int StudentFeeId,
    int StudentId,
    string StudentNumber,
    string FirstName,
    string LastName,
    string AcademicLevelName,
    string StudentGroupName,
    int PaymentTypeId,
    string PaymentTypeName,
    int? Month,
    int? Year,
    string PeriodLabel,
    decimal ExpectedAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime DueDate,
    FeeStatus Status,
    bool IsMandatory,
    string? Description = null)
{
    public string StudentName => $"{FirstName} {LastName}".Trim();
}

public record FeeFilter(
    int? AcademicLevelId = null,
    int? StudentGroupId = null,
    int? StudentId = null,
    int? PaymentTypeId = null,
    int? Month = null,
    int? Year = null,
    FeeStatus? Status = null,
    bool OnlyOutstanding = true,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 25,
    PaymentTypeScope? Scope = null);

public record FeeSummary(
    decimal TotalExpected,
    decimal TotalPaid,
    decimal TotalRemaining,
    int UnpaidStudentCount,
    int PartiallyPaidStudentCount,
    int OverdueStudentCount,
    int OverdueItemCount)
{
    public static FeeSummary Empty => new(0m, 0m, 0m, 0, 0, 0, 0);

    public decimal CollectionRate => TotalExpected <= 0 ? 0m : Math.Round(TotalPaid / TotalExpected * 100m, 1);
}

public record MonthlyFeeCell(
    int? StudentFeeId,
    int Month,
    int Year,
    string PeriodLabel,
    decimal ExpectedAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime? DueDate,
    FeeStatus? Status)
{
    public bool IsBilled => StudentFeeId.HasValue;
}

public record CreateStudentFeeRequest(
    int StudentId,
    int PaymentTypeId,
    decimal ExpectedAmount,
    DateTime DueDate,
    int? Month,
    int? Year,
    int? SchoolYearId = null,
    bool IsMandatory = true,
    string? Notes = null);

public record AdjustStudentFeeRequest(
    int StudentFeeId,
    decimal ExpectedAmount,
    DateTime DueDate,
    string? Notes);

public record GenerateMonthlyFeesRequest(
    int PaymentTypeId,
    decimal AmountPerMonth,
    IReadOnlyList<int> Months,
    int Year,
    int DueDay,
    int? AcademicLevelId,
    int? StudentGroupId,
    IReadOnlyList<int>? StudentIds = null,
    bool OnlyActiveStudents = true);

public record GenerateOneTimeFeeRequest(
    int PaymentTypeId,
    string? Description,
    decimal Amount,
    DateTime DueDate,
    int SchoolYearId,
    int? AcademicLevelId,
    int? StudentGroupId,
    IReadOnlyList<int>? StudentIds,
    bool OnlyActiveStudents = true);

public record FeeGenerationResult(
    int StudentsProcessed,
    int ItemsCreated,
    int ItemsSkipped,
    decimal TotalBilled,
    IReadOnlyList<int> CreatedFeeIds);
