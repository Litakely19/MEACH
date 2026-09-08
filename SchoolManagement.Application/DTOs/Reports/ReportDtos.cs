using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Reports;

public record ReportFilter(
    DateTime? From = null,
    DateTime? To = null,
    int? AcademicLevelId = null,
    int? StudentGroupId = null,
    int? PaymentTypeId = null);

public record PaymentMethodBreakdownRow(PaymentMethod PaymentMethod, int PaymentCount, decimal Amount);

public record DailyPaymentReport(
    DateTime Date,
    decimal TotalCollected,
    int PaymentCount,
    IReadOnlyList<PaymentMethodBreakdownRow> ByMethod)
{
    public static DailyPaymentReport Empty(DateTime date) =>
        new(date, 0m, 0, Array.Empty<PaymentMethodBreakdownRow>());
}

public record MonthlyFinancialRow(
    int Year,
    int Month,
    string PeriodLabel,
    decimal ExpectedAmount,
    decimal CollectedAmount,
    decimal RemainingAmount);

public record MonthlyFinancialReport(
    IReadOnlyList<MonthlyFinancialRow> Rows,
    decimal TotalExpected,
    decimal TotalCollected,
    decimal TotalRemaining)
{
    public static MonthlyFinancialReport Empty =>
        new(Array.Empty<MonthlyFinancialRow>(), 0m, 0m, 0m);
}

public record GroupFinancialRow(
    int StudentGroupId,
    string StudentGroupName,
    string AcademicLevelName,
    int StudentCount,
    decimal ExpectedAmount,
    decimal CollectedAmount,
    decimal RemainingAmount)
{
    public decimal CollectionRate => ExpectedAmount <= 0 ? 0m : Math.Round(CollectedAmount / ExpectedAmount * 100m, 1);
}

public record PaymentTypeFinancialRow(
    int PaymentTypeId,
    string PaymentTypeName,
    decimal ExpectedAmount,
    decimal CollectedAmount,
    decimal RemainingAmount)
{
    public decimal CollectionRate => ExpectedAmount <= 0 ? 0m : Math.Round(CollectedAmount / ExpectedAmount * 100m, 1);
}

public record OutstandingBalanceRow(
    int StudentId,
    string StudentNumber,
    string StudentName,
    string AcademicLevelName,
    string StudentGroupName,
    decimal ExpectedAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    int OverdueItemCount,
    DateTime? OldestDueDate);

public record RecentPaymentRow(
    int PaymentId,
    string PaymentNumber,
    string StudentName,
    string PaymentTypeName,
    decimal Amount,
    DateTime PaymentDate,
    string ReceivedBy);

public record LevelCountRow(string LevelName, int StudentCount);

public record DashboardSummary(
    int TotalStudents,
    int L1Students,
    int L2Students,
    int L3Students,
    int ActiveGroups,
    int PaymentsTodayCount,
    decimal PaymentsTodayAmount,
    int PaymentsThisMonthCount,
    decimal PaymentsThisMonthAmount,
    decimal TotalCollected,
    decimal OutstandingBalance,
    int OverdueItemCount,
    int AbsentToday,
    IReadOnlyList<PaymentTypeFinancialRow> CollectedByType,
    IReadOnlyList<RecentPaymentRow> RecentPayments)
{
    public static DashboardSummary Empty =>
        new(0, 0, 0, 0, 0, 0, 0m, 0, 0m, 0m, 0m, 0, 0,
            Array.Empty<PaymentTypeFinancialRow>(),
            Array.Empty<RecentPaymentRow>());
}
