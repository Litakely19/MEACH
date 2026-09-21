using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Services.Internal;
using SchoolManagement.Application.Validators;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

/// <summary>
/// Unified ledger of individual student obligations: monthly Écolage and one-time
/// fees such as Droit, Livre and examinations.
/// </summary>
public class FeeService : IFeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public FeeService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<PagedResult<StudentFeeItem>> ListAsync(
        FeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        EnsureCanView();

        var query = BuildQuery(filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var pageSize = Math.Max(1, filter.PageSize);
        var page = Math.Max(1, filter.Page);

        var rows = await Project(
                query
                    .OrderBy(fee => fee.DueDate)
                    .ThenBy(fee => fee.Student.FirstName)
                    .ThenBy(fee => fee.Student.LastName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<StudentFeeItem>(Map(rows), totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<StudentFeeItem>> ListAllAsync(
        FeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        EnsureCanView();

        var rows = await Project(
                BuildQuery(filter)
                    .OrderBy(fee => fee.Student.AcademicLevel.Name)
                    .ThenBy(fee => fee.Student.StudentGroup.Name)
                    .ThenBy(fee => fee.Student.FirstName)
                    .ThenBy(fee => fee.Student.LastName)
                    .ThenBy(fee => fee.DueDate))
            .ToListAsync(cancellationToken);

        return Map(rows);
    }

    public async Task<FeeSummary> GetSummaryAsync(
        FeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        EnsureCanView();

        var today = DateTime.Today;
        var rows = await BuildQuery(filter)
            .Select(fee => new
            {
                fee.StudentId,
                fee.ExpectedAmount,
                fee.PaidAmount,
                fee.DueDate,
                fee.Status
            })
            .ToListAsync(cancellationToken);

        var outstanding = rows.Where(row => row.Status != FeeStatus.Cancelled && row.PaidAmount < row.ExpectedAmount).ToList();
        var overdue = outstanding.Where(row => row.DueDate.Date < today).ToList();

        return new FeeSummary(
            rows.Where(row => row.Status != FeeStatus.Cancelled).Sum(row => row.ExpectedAmount),
            rows.Where(row => row.Status != FeeStatus.Cancelled).Sum(row => row.PaidAmount),
            outstanding.Sum(row => row.ExpectedAmount - row.PaidAmount),
            outstanding.Where(row => row.PaidAmount <= 0).Select(row => row.StudentId).Distinct().Count(),
            outstanding.Where(row => row.PaidAmount > 0).Select(row => row.StudentId).Distinct().Count(),
            overdue.Select(row => row.StudentId).Distinct().Count(),
            overdue.Count);
    }

    public async Task<IReadOnlyList<StudentFeeItem>> ListOutstandingByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        EnsureCanView();

        var rows = await Project(
                BuildQuery(new FeeFilter(StudentId: studentId, OnlyOutstanding: true, PageSize: 500))
                    .OrderBy(fee => fee.DueDate))
            .ToListAsync(cancellationToken);

        return Map(rows);
    }

    public async Task<IReadOnlyList<StudentFeeItem>> ListDueSoonAsync(
        int withinDays = 7,
        CancellationToken cancellationToken = default)
    {
        EnsureCanView();

        var days = Math.Max(0, withinDays);
        var today = DateTime.Today;
        var until = today.AddDays(days);

        var rows = await Project(
                BuildQuery(new FeeFilter(OnlyOutstanding: true, PageSize: 500))
                    .Where(fee => fee.DueDate >= today && fee.DueDate < until.AddDays(1))
                    .OrderBy(fee => fee.DueDate)
                    .ThenBy(fee => fee.Student.LastName)
                    .ThenBy(fee => fee.Student.FirstName))
            .ToListAsync(cancellationToken);

        return Map(rows);
    }

    public async Task<int> CreateAsync(CreateStudentFeeRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageFees);
        RequestValidation.Ensure(request, new CreateStudentFeeRequestValidator());

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.StudentId);

        var paymentType = await _unitOfWork.PaymentTypes.GetByIdAsync(request.PaymentTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentType), request.PaymentTypeId);

        ValidatePeriod(paymentType, request.Month, request.Year);
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var schoolYearId = paymentType.Frequency == PaymentFrequency.Monthly
            ? null
            : request.SchoolYearId ?? student.SchoolYearId;

        if (paymentType.Frequency != PaymentFrequency.Monthly && schoolYearId is null)
        {
            throw new DomainException("Select the school year for this fee.");
        }

        if (schoolYearId is int yearId)
        {
            var schoolYear = await _unitOfWork.SchoolYears.GetByIdAsync(yearId, cancellationToken)
                ?? throw new NotFoundException(nameof(SchoolYear), yearId);
            if (schoolYear.IsClosed)
            {
                throw new DomainException($"School year '{schoolYear.Name}' is closed; new fees cannot be billed.");
            }
        }

        await EnsureNotDuplicateAsync(
            student.Id,
            paymentType,
            request.Month,
            request.Year,
            schoolYearId,
            excludeId: null,
            notes,
            cancellationToken);

        var fee = new StudentFee
        {
            StudentId = student.Id,
            PaymentTypeId = paymentType.Id,
            ExpectedAmount = request.ExpectedAmount,
            PaidAmount = 0m,
            DueDate = request.DueDate.Date,
            Month = paymentType.Frequency == PaymentFrequency.Monthly ? request.Month : null,
            Year = paymentType.Frequency == PaymentFrequency.Monthly ? request.Year : null,
            SchoolYearId = schoolYearId,
            IsMandatory = request.IsMandatory,
            Notes = notes,
            CreatedByUserId = _currentUser.RequireUserId(),
            CreatedAt = DateTime.Now,
            Status = FeeStatus.Unpaid
        };

        fee.Status = FeeStatusCalculator.ForFee(fee);

        await _unitOfWork.StudentFees.AddAsync(fee, cancellationToken);
        await _auditService.RecordAsync(
            AuditAction.FeeCreated,
            nameof(StudentFee),
            null,
            $"Fee '{Period.FeeLabel(paymentType.Name, fee.Month, fee.Year)}' of {Money.Format(fee.ExpectedAmount)} "
                + $"created for '{student.FullName}' ({student.StudentNumber}).",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return fee.Id;
    }

    public async Task AdjustAsync(AdjustStudentFeeRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageFees);
        RequestValidation.Ensure(request, new AdjustStudentFeeRequestValidator());

        var fee = await _unitOfWork.StudentFees.GetDetailAsync(request.StudentFeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentFee), request.StudentFeeId);

        if (fee.Status == FeeStatus.Cancelled)
        {
            throw new DomainException("A cancelled obligation cannot be adjusted.");
        }

        if (request.ExpectedAmount < fee.PaidAmount)
        {
            throw new DomainException(
                $"The expected amount cannot be lower than the {Money.Format(fee.PaidAmount)} already paid.");
        }

        fee.ExpectedAmount = request.ExpectedAmount;
        fee.DueDate = request.DueDate.Date;
        fee.Notes = string.IsNullOrWhiteSpace(request.Notes) ? request.Notes : request.Notes.Trim();
        fee.UpdatedAt = DateTime.Now;
        fee.Status = FeeStatusCalculator.ForFee(fee);

        await _auditService.RecordAsync(
            AuditAction.FeeAdjusted,
            nameof(StudentFee),
            fee.Id,
            $"Fee '{Period.FeeLabel(fee.PaymentType.Name, fee.Month, fee.Year)}' for "
                + $"'{fee.Student.FullName}' adjusted to {Money.Format(fee.ExpectedAmount)}, due {fee.DueDate:dd/MM/yyyy}.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeeGenerationResult> GenerateMonthlyAsync(
        GenerateMonthlyFeesRequest request,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageFees);
        RequestValidation.Ensure(request, new GenerateMonthlyFeesRequestValidator());

        var paymentType = await _unitOfWork.PaymentTypes.GetByIdAsync(request.PaymentTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentType), request.PaymentTypeId);

        if (paymentType.Frequency != PaymentFrequency.Monthly)
        {
            throw new DomainException($"'{paymentType.Name}' is not a monthly payment type.");
        }

        var students = await LoadTargetStudentsAsync(
            request.AcademicLevelId,
            request.StudentGroupId,
            request.StudentIds,
            request.OnlyActiveStudents,
            cancellationToken);

        var created = 0;
        var skipped = 0;
        var totalBilled = 0m;
        var createdFees = new List<StudentFee>();
        var userId = _currentUser.RequireUserId();
        var now = DateTime.Now;
        var dueDay = Math.Clamp(request.DueDay, 1, 28);

        foreach (var student in students)
        {
            foreach (var month in request.Months.Distinct())
            {
                if (await _unitOfWork.StudentFees.MonthlyExistsAsync(
                        student.Id,
                        paymentType.Id,
                        month,
                        request.Year,
                        cancellationToken: cancellationToken))
                {
                    skipped++;
                    continue;
                }

                var dueDate = new DateTime(request.Year, month, dueDay);
                var fee = new StudentFee
                {
                    StudentId = student.Id,
                    PaymentTypeId = paymentType.Id,
                    ExpectedAmount = request.AmountPerMonth,
                    PaidAmount = 0m,
                    DueDate = dueDate,
                    Month = month,
                    Year = request.Year,
                    Status = FeeStatus.Unpaid,
                    IsMandatory = true,
                    CreatedByUserId = userId,
                    CreatedAt = now
                };
                fee.Status = FeeStatusCalculator.ForFee(fee);

                await _unitOfWork.StudentFees.AddAsync(fee, cancellationToken);
                createdFees.Add(fee);
                created++;
                totalBilled += request.AmountPerMonth;
            }
        }

        await _auditService.RecordAsync(
            AuditAction.FeeCreated,
            nameof(StudentFee),
            paymentType.Name,
            $"Monthly {paymentType.Name} generated for {students.Count} student(s): {created} created, {skipped} skipped.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FeeGenerationResult(
            students.Count,
            created,
            skipped,
            totalBilled,
            createdFees.Select(fee => fee.Id).ToList());
    }

    public async Task<FeeGenerationResult> GenerateOneTimeAsync(
        GenerateOneTimeFeeRequest request,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageFees);
        RequestValidation.Ensure(request, new GenerateOneTimeFeeRequestValidator());

        var paymentType = await _unitOfWork.PaymentTypes.GetByIdAsync(request.PaymentTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentType), request.PaymentTypeId);

        var schoolYear = await _unitOfWork.SchoolYears.GetByIdAsync(request.SchoolYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(SchoolYear), request.SchoolYearId);

        if (schoolYear.IsClosed)
        {
            throw new DomainException($"School year '{schoolYear.Name}' is closed; new fees cannot be billed.");
        }

        var students = await LoadTargetStudentsAsync(
            request.AcademicLevelId,
            request.StudentGroupId,
            request.StudentIds,
            request.OnlyActiveStudents,
            cancellationToken);

        var created = 0;
        var skipped = 0;
        var totalBilled = 0m;
        var createdFees = new List<StudentFee>();
        var userId = _currentUser.RequireUserId();
        var now = DateTime.Now;
        var notes = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var requiresDescription = WellKnownPaymentTypes.RequiresDistinctDescription(paymentType.Name);

        if (requiresDescription && notes is null)
        {
            throw new DomainException(
                $"Enter a description for {paymentType.Name} so several lines in the same school year can be told apart.");
        }

        foreach (var student in students)
        {
            if (await _unitOfWork.StudentFees.OneTimeExistsAsync(
                    student.Id,
                    paymentType.Id,
                    schoolYearId: schoolYear.Id,
                    notes: notes,
                    matchNotes: requiresDescription,
                    cancellationToken: cancellationToken))
            {
                skipped++;
                continue;
            }

            var fee = new StudentFee
            {
                StudentId = student.Id,
                PaymentTypeId = paymentType.Id,
                ExpectedAmount = request.Amount,
                PaidAmount = 0m,
                DueDate = request.DueDate.Date,
                Month = null,
                Year = null,
                SchoolYearId = schoolYear.Id,
                Status = FeeStatus.Unpaid,
                IsMandatory = true,
                Notes = notes,
                CreatedByUserId = userId,
                CreatedAt = now
            };
            fee.Status = FeeStatusCalculator.ForFee(fee);

            await _unitOfWork.StudentFees.AddAsync(fee, cancellationToken);
            createdFees.Add(fee);
            created++;
            totalBilled += request.Amount;
        }

        await _auditService.RecordAsync(
            AuditAction.FeeCreated,
            nameof(StudentFee),
            paymentType.Name,
            $"One-time {paymentType.Name} ({schoolYear.Name}) generated for {students.Count} student(s): {created} created, {skipped} skipped.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FeeGenerationResult(
            students.Count,
            created,
            skipped,
            totalBilled,
            createdFees.Select(fee => fee.Id).ToList());
    }

    public async Task CancelAsync(int studentFeeId, string reason, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageFees);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A reason is required to cancel a payment obligation.");
        }

        var fee = await _unitOfWork.StudentFees.GetDetailAsync(studentFeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentFee), studentFeeId);

        if (fee.Status == FeeStatus.Cancelled)
        {
            throw new DomainException("This obligation is already cancelled.");
        }

        if (fee.PaidAmount > 0)
        {
            throw new DomainException(
                "This obligation already has payments. Reverse or cancel those payments before cancelling the fee.");
        }

        fee.Status = FeeStatus.Cancelled;
        fee.Notes = string.IsNullOrWhiteSpace(fee.Notes)
            ? reason.Trim()
            : $"{fee.Notes} | Cancelled: {reason.Trim()}";
        fee.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.FeeCancelled,
            nameof(StudentFee),
            fee.Id,
            $"Fee '{Period.FeeLabel(fee.PaymentType.Name, fee.Month, fee.Year)}' for "
                + $"'{fee.Student.FullName}' cancelled. Reason: {reason.Trim()}",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RefreshOverdueStatusesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var fees = await _unitOfWork.StudentFees.Query(trackChanges: true)
            .Where(fee => fee.Status != FeeStatus.Cancelled
                && fee.Status != FeeStatus.Paid
                && fee.PaidAmount < fee.ExpectedAmount
                && fee.DueDate < today)
            .ToListAsync(cancellationToken);

        foreach (var fee in fees)
        {
            fee.Status = FeeStatusCalculator.ForFee(fee, today);
            fee.UpdatedAt = DateTime.Now;
        }

        if (fees.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return fees.Count;
    }

    private void EnsureCanView()
    {
        if (!_currentUser.HasPermission(Permission.ViewFees)
            && !_currentUser.HasPermission(Permission.ViewPayments))
        {
            _currentUser.EnsurePermission(Permission.ViewFees);
        }
    }

    private IQueryable<StudentFee> BuildQuery(FeeFilter filter)
    {
        var query = _unitOfWork.StudentFees.Query();

        if (filter.AcademicLevelId.HasValue)
        {
            query = query.Where(fee => fee.Student.AcademicLevelId == filter.AcademicLevelId);
        }

        if (filter.StudentGroupId.HasValue)
        {
            query = query.Where(fee => fee.Student.StudentGroupId == filter.StudentGroupId);
        }

        if (filter.StudentId.HasValue)
        {
            query = query.Where(fee => fee.StudentId == filter.StudentId);
        }

        if (filter.PaymentTypeId.HasValue)
        {
            query = query.Where(fee => fee.PaymentTypeId == filter.PaymentTypeId);
        }

        if (filter.Month.HasValue)
        {
            query = query.Where(fee => fee.Month == filter.Month);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(fee => fee.Year == filter.Year);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(fee => fee.Status == filter.Status);
        }
        else
        {
            query = query.Where(fee => fee.Status != FeeStatus.Cancelled);
        }

        if (filter.OnlyOutstanding)
        {
            query = query.Where(fee => fee.PaidAmount < fee.ExpectedAmount);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var names = PersonNameSearch.Parse(filter.SearchTerm);
            var lower = names.Lower;
            var firstPart = names.FirstPart;
            var lastPart = names.LastPart;
            var hasTwoParts = names.HasTwoParts;

            query = query.Where(fee =>
                fee.Student.StudentNumber.ToLower().Contains(lower)
                || fee.Student.FirstName.ToLower().Contains(lower)
                || fee.Student.LastName.ToLower().Contains(lower)
                || (fee.Student.FirstName + " " + fee.Student.LastName).ToLower().Contains(lower)
                || (fee.Student.LastName + " " + fee.Student.FirstName).ToLower().Contains(lower)
                || (hasTwoParts
                    && fee.Student.FirstName.ToLower().Contains(firstPart)
                    && fee.Student.LastName.ToLower().Contains(lastPart))
                || fee.PaymentType.Name.ToLower().Contains(lower));
        }

        if (filter.Scope is { } scope && !ReferenceEquals(scope, PaymentTypeScope.All))
        {
            if (!string.IsNullOrWhiteSpace(scope.ExactName))
            {
                var exact = scope.ExactName;
                query = query.Where(fee => fee.PaymentType.Name == exact);
            }

            if (scope.Frequencies is { Count: > 0 } frequencies)
            {
                query = query.Where(fee => frequencies.Contains(fee.PaymentType.Frequency));
            }

            if (!string.IsNullOrWhiteSpace(scope.NameContains))
            {
                var contains = scope.NameContains;
                query = query.Where(fee => fee.PaymentType.Name.Contains(contains));
            }

            if (!string.IsNullOrWhiteSpace(scope.NameDoesNotContain))
            {
                var excluded = scope.NameDoesNotContain;
                query = query.Where(fee => !fee.PaymentType.Name.Contains(excluded));
            }

            if (scope.ExcludedExactNames is { Count: > 0 } excludedNames)
            {
                query = query.Where(fee => !excludedNames.Contains(fee.PaymentType.Name));
            }
        }

        return query;
    }

    private static IQueryable<FeeRow> Project(IQueryable<StudentFee> query) =>
        query.Select(fee => new FeeRow(
            fee.Id,
            fee.StudentId,
            fee.Student.StudentNumber,
            fee.Student.FirstName,
            fee.Student.LastName,
            fee.Student.AcademicLevel.Name,
            fee.Student.StudentGroup.Name,
            fee.PaymentTypeId,
            fee.PaymentType.Name,
            fee.Month,
            fee.Year,
            fee.ExpectedAmount,
            fee.PaidAmount,
            fee.DueDate,
            fee.Status,
            fee.IsMandatory,
            fee.Notes));

    private static IReadOnlyList<StudentFeeItem> Map(IEnumerable<FeeRow> rows) =>
        rows.Select(row => new StudentFeeItem(
            row.StudentFeeId,
            row.StudentId,
            row.StudentNumber,
            row.FirstName,
            row.LastName,
            row.AcademicLevelName,
            row.StudentGroupName,
            row.PaymentTypeId,
            row.PaymentTypeName,
            row.Month,
            row.Year,
            Period.FeeLabel(row.PaymentTypeName, row.Month, row.Year),
            row.ExpectedAmount,
            row.PaidAmount,
            row.ExpectedAmount - row.PaidAmount,
            row.DueDate,
            row.Status,
            row.IsMandatory,
            row.Notes)).ToList();

    private async Task<List<Student>> LoadTargetStudentsAsync(
        int? academicLevelId,
        int? studentGroupId,
        IReadOnlyList<int>? studentIds,
        bool onlyActive,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Students.Query();

        if (onlyActive)
        {
            query = query.Where(student => student.Status == StudentStatus.Active);
        }

        if (academicLevelId.HasValue)
        {
            query = query.Where(student => student.AcademicLevelId == academicLevelId);
        }

        if (studentGroupId.HasValue)
        {
            query = query.Where(student => student.StudentGroupId == studentGroupId);
        }

        if (studentIds is { Count: > 0 })
        {
            query = query.Where(student => studentIds.Contains(student.Id));
        }

        var students = await query.OrderBy(student => student.LastName).ThenBy(student => student.FirstName)
            .ToListAsync(cancellationToken);

        if (students.Count == 0)
        {
            throw new DomainException("No students match the selected criteria.");
        }

        return students;
    }

    private static void ValidatePeriod(PaymentType paymentType, int? month, int? year)
    {
        if (paymentType.Frequency == PaymentFrequency.Monthly)
        {
            if (month is null or < 1 or > 12 || year is null or < 2000)
            {
                throw new DomainException($"Month and year are required for {paymentType.Name}.");
            }
        }
    }

    private async Task EnsureNotDuplicateAsync(
        int studentId,
        PaymentType paymentType,
        int? month,
        int? year,
        int? schoolYearId,
        int? excludeId,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (paymentType.Frequency == PaymentFrequency.Monthly && month.HasValue && year.HasValue)
        {
            if (await _unitOfWork.StudentFees.MonthlyExistsAsync(
                    studentId,
                    paymentType.Id,
                    month.Value,
                    year.Value,
                    excludeId,
                    cancellationToken))
            {
                throw new DomainException(
                    $"{paymentType.Name} for {Period.Label(month, year)} already exists for this student.");
            }

            return;
        }

        var isDescriptionScoped = WellKnownPaymentTypes.RequiresDistinctDescription(paymentType.Name);

        if (isDescriptionScoped && string.IsNullOrWhiteSpace(notes))
        {
            throw new DomainException(
                $"Enter a description for {paymentType.Name} so several lines in the same school year can be told apart.");
        }

        if (await _unitOfWork.StudentFees.OneTimeExistsAsync(
                studentId,
                paymentType.Id,
                schoolYearId: schoolYearId,
                excludeId: excludeId,
                notes: notes,
                matchNotes: isDescriptionScoped,
                cancellationToken: cancellationToken))
        {
            var scope = schoolYearId.HasValue ? " for this school year" : string.Empty;
            throw new DomainException(
                isDescriptionScoped
                    ? $"{paymentType.Name} '{notes}' already exists for this student{scope}."
                    : $"{paymentType.Name} already exists for this student{scope}.");
        }
    }

    private sealed record FeeRow(
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
        decimal ExpectedAmount,
        decimal PaidAmount,
        DateTime DueDate,
        FeeStatus Status,
        bool IsMandatory,
        string? Notes);
}
