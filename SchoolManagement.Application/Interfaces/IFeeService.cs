using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;

namespace SchoolManagement.Application.Interfaces;

public interface IFeeService
{
    Task<PagedResult<StudentFeeItem>> ListAsync(FeeFilter filter, CancellationToken cancellationToken = default);

    Task<FeeSummary> GetSummaryAsync(FeeFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentFeeItem>> ListAllAsync(FeeFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentFeeItem>> ListOutstandingByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    /// <summary>Outstanding fees whose due date falls within the next <paramref name="withinDays"/> days (inclusive of today).</summary>
    Task<IReadOnlyList<StudentFeeItem>> ListDueSoonAsync(
        int withinDays = 7,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateStudentFeeRequest request, CancellationToken cancellationToken = default);

    Task AdjustAsync(AdjustStudentFeeRequest request, CancellationToken cancellationToken = default);

    Task<FeeGenerationResult> GenerateMonthlyAsync(
        GenerateMonthlyFeesRequest request,
        CancellationToken cancellationToken = default);

    Task<FeeGenerationResult> GenerateOneTimeAsync(
        GenerateOneTimeFeeRequest request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(int studentFeeId, string reason, CancellationToken cancellationToken = default);

    Task<int> RefreshOverdueStatusesAsync(CancellationToken cancellationToken = default);
}
