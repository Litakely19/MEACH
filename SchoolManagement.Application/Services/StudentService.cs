using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.Students;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Services.Internal;
using SchoolManagement.Application.Validators;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public StudentService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<PagedResult<StudentListItem>> ListAsync(
        StudentFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewStudents);

        var query = _unitOfWork.Students.Query();

        if (filter.AcademicLevelId.HasValue)
        {
            query = query.Where(student => student.AcademicLevelId == filter.AcademicLevelId);
        }

        if (filter.StudentGroupId.HasValue)
        {
            query = query.Where(student => student.StudentGroupId == filter.StudentGroupId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(student => student.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var names = PersonNameSearch.Parse(filter.SearchTerm);
            var lower = names.Lower;
            var firstPart = names.FirstPart;
            var lastPart = names.LastPart;
            var hasTwoParts = names.HasTwoParts;

            query = query.Where(student =>
                student.StudentNumber.ToLower().Contains(lower)
                || student.FirstName.ToLower().Contains(lower)
                || student.LastName.ToLower().Contains(lower)
                || (student.FirstName + " " + student.LastName).ToLower().Contains(lower)
                || (student.LastName + " " + student.FirstName).ToLower().Contains(lower)
                || (hasTwoParts
                    && student.FirstName.ToLower().Contains(firstPart)
                    && student.LastName.ToLower().Contains(lastPart))
                || (student.PhoneNumber != null && student.PhoneNumber.ToLower().Contains(lower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageSize = Math.Max(1, filter.PageSize);
        var page = Math.Max(1, filter.Page);

        var rows = await query
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(student => new
            {
                student.Id,
                student.StudentNumber,
                student.FirstName,
                student.LastName,
                student.Gender,
                student.DateOfBirth,
                LevelName = student.AcademicLevel.Name,
                GroupName = student.StudentGroup.Name,
                student.PhoneNumber,
                student.Status,
                student.EnrollmentDate
            })
            .ToListAsync(cancellationToken);

        var balances = await StudentBalanceLookup.GetOutstandingAsync(
            _unitOfWork,
            rows.Select(row => row.Id).ToList(),
            cancellationToken);

        var items = rows
            .Select(row => new StudentListItem(
                row.Id,
                row.StudentNumber,
                $"{row.FirstName} {row.LastName}".Trim(),
                row.Gender,
                row.DateOfBirth,
                row.LevelName,
                row.GroupName,
                row.PhoneNumber,
                row.Status,
                row.EnrollmentDate,
                balances.GetValueOrDefault(row.Id)))
            .ToList();

        return new PagedResult<StudentListItem>(items, totalCount, page, pageSize);
    }

    public async Task<StudentDetail> GetDetailAsync(int studentId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewStudents);

        var student = await _unitOfWork.Students.GetDetailAsync(studentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), studentId);

        var fees = await _unitOfWork.StudentFees.Query()
            .Where(fee => fee.StudentId == studentId && fee.Status != FeeStatus.Cancelled)
            .OrderBy(fee => fee.DueDate)
            .Select(fee => new
            {
                fee.Id,
                PaymentTypeName = fee.PaymentType.Name,
                fee.Month,
                fee.Year,
                fee.ExpectedAmount,
                fee.PaidAmount,
                fee.DueDate,
                fee.Status
            })
            .ToListAsync(cancellationToken);

        var payments = await _unitOfWork.Payments.Query()
            .Where(payment => payment.StudentId == studentId)
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.Id)
            .Select(payment => new
            {
                payment.Id,
                payment.PaymentNumber,
                payment.PaymentDate,
                payment.Amount,
                payment.PaymentMethod,
                ReceivedBy = payment.ReceivedByUser.FirstName + " " + payment.ReceivedByUser.LastName,
                payment.Status,
                ReceiptId = payment.Receipt == null ? (int?)null : payment.Receipt.Id,
                ReceiptNumber = payment.Receipt == null ? null : payment.Receipt.ReceiptNumber,
                ReceiptIssueDate = payment.Receipt == null ? (DateTime?)null : payment.Receipt.IssueDate
            })
            .ToListAsync(cancellationToken);

        var attendanceRows = await _unitOfWork.Attendances.Query()
            .Where(attendance => attendance.StudentId == studentId)
            .Select(attendance => attendance.Status)
            .ToListAsync(cancellationToken);

        var present = attendanceRows.Count(status => status == AttendanceStatus.Present);
        var absent = attendanceRows.Count(status => status == AttendanceStatus.Absent);
        var late = attendanceRows.Count(status => status == AttendanceStatus.Late);
        var excused = attendanceRows.Count(status => status == AttendanceStatus.Excused);
        var totalSessions = attendanceRows.Count;
        var attendancePercentage = totalSessions == 0
            ? 0m
            : Math.Round(present / (decimal)totalSessions * 100m, 1);

        var feeSummaries = fees
            .Select(fee => new StudentFeeSummary(
                fee.Id,
                fee.PaymentTypeName,
                Period.FeeLabel(fee.PaymentTypeName, fee.Month, fee.Year),
                fee.ExpectedAmount,
                fee.PaidAmount,
                fee.ExpectedAmount - fee.PaidAmount,
                fee.DueDate,
                fee.Status))
            .ToList();

        var paymentSummaries = payments
            .Select(payment => new StudentPaymentSummary(
                payment.Id,
                payment.PaymentNumber,
                payment.PaymentDate,
                payment.Amount,
                payment.PaymentMethod,
                payment.ReceivedBy,
                payment.Status,
                payment.ReceiptNumber))
            .ToList();

        var receiptSummaries = payments
            .Where(payment => payment.ReceiptId.HasValue)
            .Select(payment => new StudentReceiptSummary(
                payment.ReceiptId!.Value,
                payment.ReceiptNumber!,
                payment.ReceiptIssueDate ?? payment.PaymentDate,
                payment.Amount,
                payment.PaymentNumber))
            .ToList();

        var schedule = student.StudentGroup.Schedules
            .OrderBy(item => item.DayOfWeek)
            .ThenBy(item => item.StartTime)
            .Select(item => new StudentScheduleItem(item.DayOfWeek, item.StartTime, item.EndTime, item.SessionLabel))
            .ToList();

        var totalExpected = feeSummaries.Sum(fee => fee.ExpectedAmount);
        var totalPaid = feeSummaries.Sum(fee => fee.PaidAmount);

        return new StudentDetail(
            student.Id,
            student.StudentNumber,
            student.FirstName,
            student.LastName,
            student.FullName,
            student.Gender,
            student.DateOfBirth,
            student.Address,
            student.PhoneNumber,
            student.Email,
            student.AcademicLevelId,
            student.AcademicLevel.Name,
            student.StudentGroupId,
            student.StudentGroup.Name,
            student.SchoolYearId,
            student.SchoolYear?.Name,
            student.EnrollmentDate,
            student.Status,
            student.CreatedAt,
            student.UpdatedAt,
            schedule,
            new StudentAttendanceSummary(totalSessions, present, absent, late, excused, attendancePercentage),
            feeSummaries,
            paymentSummaries,
            receiptSummaries,
            totalExpected,
            totalPaid,
            totalExpected - totalPaid);
    }

    public async Task<IReadOnlyList<MonthlyFeeCell>> GetMonthlyFeeTimelineAsync(
        int studentId,
        int year,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewStudents);

        var billed = await _unitOfWork.StudentFees.Query()
            .Where(fee => fee.StudentId == studentId
                && fee.Year == year
                && fee.Month != null
                && fee.Status != FeeStatus.Cancelled)
            .Select(fee => new
            {
                fee.Id,
                Month = fee.Month!.Value,
                Year = fee.Year!.Value,
                fee.ExpectedAmount,
                fee.PaidAmount,
                fee.DueDate,
                fee.Status
            })
            .ToListAsync(cancellationToken);

        var cells = new List<MonthlyFeeCell>();

        for (var month = 1; month <= 12; month++)
        {
            var monthItems = billed.Where(item => item.Month == month).ToList();
            if (monthItems.Count == 0)
            {
                cells.Add(new MonthlyFeeCell(null, month, year, Period.Label(month, year), 0m, 0m, 0m, null, null));
                continue;
            }

            var expected = monthItems.Sum(item => item.ExpectedAmount);
            var paid = monthItems.Sum(item => item.PaidAmount);

            cells.Add(new MonthlyFeeCell(
                monthItems[0].Id,
                month,
                year,
                Period.Label(month, year),
                expected,
                paid,
                expected - paid,
                monthItems.Min(item => item.DueDate),
                AggregateStatus(monthItems.Select(item => item.Status))));
        }

        return cells;
    }

    public async Task<int> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudents);
        RequestValidation.Ensure(request, new CreateStudentRequestValidator());

        await EnsureGroupMatchesLevelAsync(request.StudentGroupId, request.AcademicLevelId, cancellationToken);

        if (request.SchoolYearId.HasValue)
        {
            _ = await _unitOfWork.SchoolYears.GetByIdAsync(request.SchoolYearId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(SchoolYear), request.SchoolYearId.Value);
        }

        var student = new Student
        {
            StudentNumber = await GenerateStudentNumberAsync(cancellationToken),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth.Date,
            Address = Normalize(request.Address),
            PhoneNumber = Normalize(request.PhoneNumber),
            Email = Normalize(request.Email),
            AcademicLevelId = request.AcademicLevelId,
            StudentGroupId = request.StudentGroupId,
            SchoolYearId = request.SchoolYearId,
            EnrollmentDate = request.EnrollmentDate.Date,
            Status = request.Status,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Students.AddAsync(student, cancellationToken);
        await _auditService.RecordAsync(
            AuditAction.Created,
            nameof(Student),
            student.StudentNumber,
            $"Student '{student.FullName}' ({student.StudentNumber}) enrolled.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return student.Id;
    }

    public async Task UpdateAsync(UpdateStudentRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudents);

        var student = await _unitOfWork.Students.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.Id);

        RequestValidation.Ensure(request, new UpdateStudentRequestValidator());
        await EnsureGroupMatchesLevelAsync(request.StudentGroupId, request.AcademicLevelId, cancellationToken);

        if (request.SchoolYearId.HasValue)
        {
            _ = await _unitOfWork.SchoolYears.GetByIdAsync(request.SchoolYearId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(SchoolYear), request.SchoolYearId.Value);
        }

        var groupChanged = student.StudentGroupId != request.StudentGroupId;

        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.Gender = request.Gender;
        student.DateOfBirth = request.DateOfBirth.Date;
        student.Address = Normalize(request.Address);
        student.PhoneNumber = Normalize(request.PhoneNumber);
        student.Email = Normalize(request.Email);
        student.AcademicLevelId = request.AcademicLevelId;
        student.StudentGroupId = request.StudentGroupId;
        student.SchoolYearId = request.SchoolYearId;
        student.EnrollmentDate = request.EnrollmentDate.Date;
        student.Status = request.Status;
        student.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            groupChanged ? AuditAction.StudentTransferred : AuditAction.Updated,
            nameof(Student),
            student.StudentNumber,
            groupChanged
                ? $"Student '{student.FullName}' moved to group {request.StudentGroupId}."
                : $"Student '{student.FullName}' ({student.StudentNumber}) updated.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferAsync(TransferStudentRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudents);

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.StudentId);

        var targetGroup = await _unitOfWork.StudentGroups.GetDetailAsync(request.TargetStudentGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), request.TargetStudentGroupId);

        if (!targetGroup.IsActive)
        {
            throw new DomainException($"The group '{targetGroup.Name}' is not active.");
        }

        if (student.StudentGroupId == targetGroup.Id)
        {
            throw new DomainException($"The student already belongs to '{targetGroup.Name}'.");
        }

        var previousGroup = await _unitOfWork.StudentGroups.GetByIdAsync(student.StudentGroupId, cancellationToken);
        var previousName = previousGroup?.Name ?? "-";

        student.StudentGroupId = targetGroup.Id;
        student.AcademicLevelId = targetGroup.AcademicLevelId;
        student.UpdatedAt = DateTime.Now;

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : $" Reason: {request.Reason.Trim()}";

        await _auditService.RecordAsync(
            AuditAction.StudentTransferred,
            nameof(Student),
            student.StudentNumber,
            $"Student '{student.FullName}' transferred from {previousName} to {targetGroup.Name}.{reason}",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int studentId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudents);

        var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), studentId);

        _unitOfWork.Students.Remove(student);

        await _auditService.RecordAsync(
            AuditAction.Deleted,
            nameof(Student),
            student.StudentNumber,
            $"Student '{student.FullName}' ({student.StudentNumber}) removed; financial history preserved.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> PeekNextStudentNumberAsync(CancellationToken cancellationToken = default) =>
        await GenerateStudentNumberAsync(cancellationToken);

    private async Task<string> GenerateStudentNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.Today.Year;
        var currentYear = await _unitOfWork.SchoolYears.Query()
            .Where(item => item.IsCurrent)
            .Select(item => (int?)item.StartDate.Year)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentYear.HasValue)
        {
            year = currentYear.Value;
        }

        var prefix = DocumentNumber.BuildPrefix(DocumentCodes.Student, year);
        var last = await _unitOfWork.Students.GetLastStudentNumberAsync(prefix, cancellationToken);
        return DocumentNumber.Next(DocumentCodes.Student, year, last);
    }

    private async Task EnsureGroupMatchesLevelAsync(
        int studentGroupId,
        int academicLevelId,
        CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.StudentGroups.GetByIdAsync(studentGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), studentGroupId);

        if (group.AcademicLevelId != academicLevelId)
        {
            throw new DomainException($"The group '{group.Name}' does not belong to the selected academic level.");
        }

        if (!group.IsActive)
        {
            throw new DomainException($"The group '{group.Name}' is not active.");
        }

        var level = await _unitOfWork.AcademicLevels.GetByIdAsync(academicLevelId, cancellationToken)
            ?? throw new NotFoundException(nameof(AcademicLevel), academicLevelId);

        if (!level.IsActive)
        {
            throw new DomainException($"The academic level '{level.Name}' is not active.");
        }
    }

    private static FeeStatus AggregateStatus(IEnumerable<FeeStatus> statuses)
    {
        var list = statuses.ToList();

        if (list.All(status => status == FeeStatus.Paid))
        {
            return FeeStatus.Paid;
        }

        if (list.Contains(FeeStatus.Overdue))
        {
            return FeeStatus.Overdue;
        }

        return list.Contains(FeeStatus.PartiallyPaid) || list.Contains(FeeStatus.Paid)
            ? FeeStatus.PartiallyPaid
            : FeeStatus.Unpaid;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
