using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

/// <summary>
/// Cash collected comes from active payments. Expected, remaining and overdue
/// figures come from individual StudentFee obligations.
/// </summary>
public class ReportService : IReportService
{
    private const int RecentPaymentCount = 8;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ReportService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewDashboard);

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var students = await _unitOfWork.Students.Query()
            .Select(student => new { student.Id, student.Status, LevelName = student.AcademicLevel.Name })
            .ToListAsync(cancellationToken);

        var totalStudents = students.Count;
        var l1 = students.Count(student => student.LevelName == WellKnownAcademicLevels.L1);
        var l2 = students.Count(student => student.LevelName == WellKnownAcademicLevels.L2);
        var l3 = students.Count(student => student.LevelName == WellKnownAcademicLevels.L3);

        var activeGroups = await _unitOfWork.StudentGroups.Query()
            .CountAsync(group => group.IsActive, cancellationToken);

        var paymentRows = await _unitOfWork.Payments.Query()
            .Where(payment => payment.Status == PaymentStatus.Active)
            .Select(payment => new
            {
                payment.Id,
                payment.PaymentNumber,
                payment.Amount,
                payment.PaymentDate,
                payment.CreatedAt,
                StudentName = payment.Student.FirstName + " " + payment.Student.LastName,
                PaymentTypeName = payment.StudentFee.PaymentType.Name,
                ReceivedBy = payment.ReceivedByUser.FirstName + " " + payment.ReceivedByUser.LastName
            })
            .ToListAsync(cancellationToken);

        var todayRows = paymentRows.Where(row => row.PaymentDate.Date == today).ToList();
        var monthRows = paymentRows
            .Where(row => row.PaymentDate >= monthStart && row.PaymentDate < nextMonthStart)
            .ToList();

        var feeRows = await BuildFeeQuery(new ReportFilter())
            .Select(fee => new
            {
                fee.ExpectedAmount,
                fee.PaidAmount,
                fee.DueDate,
                PaymentTypeId = fee.PaymentTypeId,
                PaymentTypeName = fee.PaymentType.Name
            })
            .ToListAsync(cancellationToken);

        var outstanding = feeRows.Where(row => row.PaidAmount < row.ExpectedAmount).ToList();
        var overdue = outstanding.Where(row => row.DueDate.Date < today).ToList();

        var collectedByType = feeRows
            .GroupBy(row => new { row.PaymentTypeId, row.PaymentTypeName })
            .Select(group => new PaymentTypeFinancialRow(
                group.Key.PaymentTypeId,
                group.Key.PaymentTypeName,
                group.Sum(row => row.ExpectedAmount),
                group.Sum(row => row.PaidAmount),
                group.Sum(row => row.ExpectedAmount - row.PaidAmount)))
            .OrderBy(row => row.PaymentTypeName)
            .ToList();

        var recentPayments = paymentRows
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.Id)
            .Take(RecentPaymentCount)
            .Select(row => new RecentPaymentRow(
                row.Id,
                row.PaymentNumber,
                row.StudentName,
                row.PaymentTypeName,
                row.Amount,
                row.PaymentDate,
                row.ReceivedBy))
            .ToList();

        var absentToday = await _unitOfWork.Attendances.Query()
            .CountAsync(
                attendance => attendance.AttendanceDate == today && attendance.Status == AttendanceStatus.Absent,
                cancellationToken);

        return new DashboardSummary(
            totalStudents,
            l1,
            l2,
            l3,
            activeGroups,
            todayRows.Count,
            todayRows.Sum(row => row.Amount),
            monthRows.Count,
            monthRows.Sum(row => row.Amount),
            paymentRows.Sum(row => row.Amount),
            outstanding.Sum(row => row.ExpectedAmount - row.PaidAmount),
            overdue.Count,
            absentToday,
            collectedByType,
            recentPayments);
    }

    public async Task<DailyPaymentReport> GetDailyPaymentsAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReports);

        var day = date.Date;
        var nextDay = day.AddDays(1);

        var rows = await _unitOfWork.Payments.Query()
            .Where(payment => payment.Status == PaymentStatus.Active
                && payment.PaymentDate >= day
                && payment.PaymentDate < nextDay)
            .Select(payment => new { payment.Amount, payment.PaymentMethod })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return DailyPaymentReport.Empty(day);
        }

        var byMethod = rows
            .GroupBy(row => row.PaymentMethod)
            .Select(group => new PaymentMethodBreakdownRow(
                group.Key,
                group.Count(),
                group.Sum(row => row.Amount)))
            .OrderByDescending(row => row.Amount)
            .ToList();

        return new DailyPaymentReport(day, rows.Sum(row => row.Amount), rows.Count, byMethod);
    }

    public async Task<MonthlyFinancialReport> GetMonthlyFinancialReportAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReports);

        var rows = await BuildFeeQuery(filter)
            .Where(fee => fee.Month != null && fee.Year != null)
            .Select(fee => new
            {
                Month = fee.Month!.Value,
                Year = fee.Year!.Value,
                fee.ExpectedAmount,
                fee.PaidAmount
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return MonthlyFinancialReport.Empty;
        }

        var monthlyRows = rows
            .GroupBy(row => new { row.Year, row.Month })
            .Select(group => new MonthlyFinancialRow(
                group.Key.Year,
                group.Key.Month,
                Period.Label(group.Key.Month, group.Key.Year),
                group.Sum(row => row.ExpectedAmount),
                group.Sum(row => row.PaidAmount),
                group.Sum(row => row.ExpectedAmount - row.PaidAmount)))
            .OrderBy(row => row.Year)
            .ThenBy(row => row.Month)
            .ToList();

        return new MonthlyFinancialReport(
            monthlyRows,
            monthlyRows.Sum(row => row.ExpectedAmount),
            monthlyRows.Sum(row => row.CollectedAmount),
            monthlyRows.Sum(row => row.RemainingAmount));
    }

    public async Task<IReadOnlyList<GroupFinancialRow>> GetPaymentsByGroupAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReports);

        var rows = await BuildFeeQuery(filter)
            .Select(fee => new
            {
                fee.Student.StudentGroupId,
                GroupName = fee.Student.StudentGroup.Name,
                LevelName = fee.Student.AcademicLevel.Name,
                fee.StudentId,
                fee.ExpectedAmount,
                fee.PaidAmount
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new { row.StudentGroupId, row.GroupName, row.LevelName })
            .Select(group => new GroupFinancialRow(
                group.Key.StudentGroupId,
                group.Key.GroupName,
                group.Key.LevelName,
                group.Select(row => row.StudentId).Distinct().Count(),
                group.Sum(row => row.ExpectedAmount),
                group.Sum(row => row.PaidAmount),
                group.Sum(row => row.ExpectedAmount - row.PaidAmount)))
            .OrderBy(row => row.AcademicLevelName)
            .ThenBy(row => row.StudentGroupName)
            .ToList();
    }

    public async Task<IReadOnlyList<PaymentTypeFinancialRow>> GetPaymentsByTypeAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReports);

        var rows = await BuildFeeQuery(filter)
            .Select(fee => new
            {
                fee.PaymentTypeId,
                PaymentTypeName = fee.PaymentType.Name,
                fee.ExpectedAmount,
                fee.PaidAmount
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new { row.PaymentTypeId, row.PaymentTypeName })
            .Select(group => new PaymentTypeFinancialRow(
                group.Key.PaymentTypeId,
                group.Key.PaymentTypeName,
                group.Sum(row => row.ExpectedAmount),
                group.Sum(row => row.PaidAmount),
                group.Sum(row => row.ExpectedAmount - row.PaidAmount)))
            .OrderBy(row => row.PaymentTypeName)
            .ToList();
    }

    public async Task<IReadOnlyList<OutstandingBalanceRow>> GetOutstandingBalancesAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReports);

        var today = DateTime.Today;
        var rows = await BuildFeeQuery(filter)
            .Where(fee => fee.PaidAmount < fee.ExpectedAmount)
            .Select(fee => new
            {
                fee.StudentId,
                fee.Student.StudentNumber,
                StudentName = fee.Student.FirstName + " " + fee.Student.LastName,
                LevelName = fee.Student.AcademicLevel.Name,
                GroupName = fee.Student.StudentGroup.Name,
                fee.ExpectedAmount,
                fee.PaidAmount,
                fee.DueDate
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new { row.StudentId, row.StudentNumber, row.StudentName, row.LevelName, row.GroupName })
            .Select(group => new OutstandingBalanceRow(
                group.Key.StudentId,
                group.Key.StudentNumber,
                group.Key.StudentName,
                group.Key.LevelName,
                group.Key.GroupName,
                group.Sum(row => row.ExpectedAmount),
                group.Sum(row => row.PaidAmount),
                group.Sum(row => row.ExpectedAmount - row.PaidAmount),
                group.Count(row => row.DueDate.Date < today),
                group.Min(row => row.DueDate)))
            .OrderByDescending(row => row.RemainingAmount)
            .ToList();
    }

    private IQueryable<Domain.Entities.StudentFee> BuildFeeQuery(ReportFilter filter)
    {
        var query = _unitOfWork.StudentFees.Query()
            .Where(fee => fee.Status != FeeStatus.Cancelled);

        if (filter.AcademicLevelId.HasValue)
        {
            query = query.Where(fee => fee.Student.AcademicLevelId == filter.AcademicLevelId);
        }

        if (filter.StudentGroupId.HasValue)
        {
            query = query.Where(fee => fee.Student.StudentGroupId == filter.StudentGroupId);
        }

        if (filter.PaymentTypeId.HasValue)
        {
            query = query.Where(fee => fee.PaymentTypeId == filter.PaymentTypeId);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.Date;
            query = query.Where(fee => fee.DueDate >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.Date.AddDays(1);
            query = query.Where(fee => fee.DueDate < to);
        }

        return query;
    }
}
