using SchoolManagement.Application.DTOs.Reports;

namespace SchoolManagement.Application.Interfaces;

public interface IReportService
{
    Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<DailyPaymentReport> GetDailyPaymentsAsync(DateTime date, CancellationToken cancellationToken = default);

    Task<MonthlyFinancialReport> GetMonthlyFinancialReportAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupFinancialRow>> GetPaymentsByGroupAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTypeFinancialRow>> GetPaymentsByTypeAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutstandingBalanceRow>> GetOutstandingBalancesAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default);
}
