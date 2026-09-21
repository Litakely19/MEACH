using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Payments;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class PaymentService : IPaymentService
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromHours(12);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ISchoolYearService _schoolYearService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ISchoolYearService schoolYearService,
        IAuditService auditService,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _schoolYearService = schoolYearService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<PagedResult<PaymentListItem>> ListAsync(
        PaymentFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewPayments);

        var query = _unitOfWork.Payments.Query();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(payment => payment.StudentId == filter.StudentId);
        }

        if (filter.AcademicLevelId.HasValue)
        {
            query = query.Where(payment => payment.Student.AcademicLevelId == filter.AcademicLevelId);
        }

        if (filter.StudentGroupId.HasValue)
        {
            query = query.Where(payment => payment.Student.StudentGroupId == filter.StudentGroupId);
        }

        if (filter.PaymentTypeId.HasValue)
        {
            query = query.Where(payment => payment.StudentFee.PaymentTypeId == filter.PaymentTypeId);
        }

        if (filter.PaymentMethod.HasValue)
        {
            query = query.Where(payment => payment.PaymentMethod == filter.PaymentMethod);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(payment => payment.Status == filter.Status);
        }

        if (filter.ReceivedByUserId.HasValue)
        {
            query = query.Where(payment => payment.ReceivedByUserId == filter.ReceivedByUserId);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.Date;
            query = query.Where(payment => payment.PaymentDate >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.Date.AddDays(1);
            query = query.Where(payment => payment.PaymentDate < to);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var names = PersonNameSearch.Parse(filter.SearchTerm);
            var lower = names.Lower;
            var firstPart = names.FirstPart;
            var lastPart = names.LastPart;
            var hasTwoParts = names.HasTwoParts;

            query = query.Where(payment =>
                payment.PaymentNumber.ToLower().Contains(lower)
                || payment.Student.StudentNumber.ToLower().Contains(lower)
                || payment.Student.FirstName.ToLower().Contains(lower)
                || payment.Student.LastName.ToLower().Contains(lower)
                || (payment.Student.FirstName + " " + payment.Student.LastName).ToLower().Contains(lower)
                || (payment.Student.LastName + " " + payment.Student.FirstName).ToLower().Contains(lower)
                || (hasTwoParts
                    && payment.Student.FirstName.ToLower().Contains(firstPart)
                    && payment.Student.LastName.ToLower().Contains(lastPart))
                || (payment.Receipt != null && payment.Receipt.ReceiptNumber.ToLower().Contains(lower))
                || (payment.Reference != null && payment.Reference.ToLower().Contains(lower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageSize = Math.Max(1, filter.PageSize);
        var page = Math.Max(1, filter.Page);

        var rows = await query
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(payment => new
            {
                payment.Id,
                payment.PaymentNumber,
                payment.StudentId,
                payment.Student.StudentNumber,
                StudentFirstName = payment.Student.FirstName,
                StudentLastName = payment.Student.LastName,
                AcademicLevelName = payment.Student.AcademicLevel.Name,
                StudentGroupName = payment.Student.StudentGroup.Name,
                PaymentTypeName = payment.StudentFee.PaymentType.Name,
                payment.StudentFee.Month,
                payment.StudentFee.Year,
                payment.Amount,
                payment.PaymentDate,
                payment.PaymentMethod,
                payment.Reference,
                ReceivedBy = payment.ReceivedByUser.FirstName + " " + payment.ReceivedByUser.LastName,
                payment.Status,
                ReceiptNumber = payment.Receipt == null ? null : payment.Receipt.ReceiptNumber
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new PaymentListItem(
                row.Id,
                row.PaymentNumber,
                row.StudentId,
                row.StudentNumber,
                row.StudentFirstName,
                row.StudentLastName,
                row.AcademicLevelName,
                row.StudentGroupName,
                row.PaymentTypeName,
                Period.FeeLabel(row.PaymentTypeName, row.Month, row.Year),
                row.Amount,
                row.PaymentDate,
                row.PaymentMethod,
                row.Reference,
                row.ReceivedBy,
                row.Status,
                row.ReceiptNumber))
            .ToList();

        return new PagedResult<PaymentListItem>(items, totalCount, page, pageSize);
    }

    public async Task<PaymentDetail> GetDetailAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewPayments);

        var payment = await _unitOfWork.Payments.GetDetailAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), paymentId);

        string? cancelledBy = null;
        if (payment.CancelledByUserId.HasValue)
        {
            cancelledBy = await _unitOfWork.Users.Query()
                .Where(user => user.Id == payment.CancelledByUserId)
                .Select(user => user.FirstName + " " + user.LastName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new PaymentDetail(
            payment.Id,
            payment.PaymentNumber,
            payment.StudentId,
            payment.Student.StudentNumber,
            payment.Student.FullName,
            payment.Student.AcademicLevel.Name,
            payment.Student.StudentGroup.Name,
            payment.StudentFeeId,
            payment.StudentFee.PaymentType.Name,
            Period.FeeLabel(payment.StudentFee.PaymentType.Name, payment.StudentFee.Month, payment.StudentFee.Year),
            payment.Amount,
            payment.PaymentDate,
            payment.PaymentMethod,
            payment.Reference,
            payment.Notes,
            payment.ReceivedByUser.FullName,
            payment.Status,
            payment.CancelledAt,
            cancelledBy,
            payment.CancellationReason,
            payment.Receipt?.Id,
            payment.Receipt?.ReceiptNumber);
    }

    public async Task<DuplicatePaymentWarning?> CheckForDuplicateAsync(
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Payments.FindPossibleDuplicateAsync(
            request.StudentId,
            request.Amount,
            request.PaymentDate,
            DuplicateWindow,
            cancellationToken);

        if (existing is null)
        {
            return null;
        }

        var receivedBy = await _unitOfWork.Users.Query()
            .Where(user => user.Id == existing.ReceivedByUserId)
            .Select(user => user.FirstName + " " + user.LastName)
            .FirstOrDefaultAsync(cancellationToken) ?? "-";

        return new DuplicatePaymentWarning(
            existing.Id,
            existing.PaymentNumber,
            existing.Amount,
            existing.PaymentDate,
            receivedBy);
    }

    public async Task<RegisterPaymentResult> RegisterAsync(
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.RegisterPayments);
        RequestValidation.Ensure(request, new RegisterPaymentRequestValidator());

        var fee = await _unitOfWork.StudentFees.Query(trackChanges: true)
            .Include(item => item.PaymentType)
            .Include(item => item.Student)
            .FirstOrDefaultAsync(item => item.Id == request.StudentFeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentFee), request.StudentFeeId);

        if (fee.StudentId != request.StudentId)
        {
            throw new DomainException("The selected obligation does not belong to this student.");
        }

        if (fee.Status == FeeStatus.Cancelled)
        {
            throw new DomainException("This payment obligation is cancelled.");
        }

        if (fee.RemainingAmount <= 0)
        {
            throw new DomainException("This obligation is already fully settled.");
        }

        if (request.Amount > fee.RemainingAmount)
        {
            throw new DomainException(
                $"The amount exceeds the remaining balance of {Money.Format(fee.RemainingAmount)}.");
        }

        if (fee.Student.SchoolYearId is int schoolYearId)
        {
            await _schoolYearService.EnsureOpenForTransactionsAsync(schoolYearId, cancellationToken);
        }

        if (!request.DuplicateConfirmed)
        {
            var duplicate = await CheckForDuplicateAsync(request, cancellationToken);
            if (duplicate is not null)
            {
                throw new DomainException(
                    $"Payment {duplicate.ExistingPaymentNumber} of {Money.Format(duplicate.Amount)} was already "
                        + $"registered for this student on {duplicate.PaymentDate:dd/MM/yyyy}. "
                        + "Confirm the operation to record it again.");
            }
        }

        var userId = _currentUser.RequireUserId();
        var now = DateTime.Now;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var payment = new Payment
            {
                PaymentNumber = await GenerateNumberAsync(
                    DocumentCodes.Payment,
                    request.PaymentDate.Year,
                    _unitOfWork.Payments.GetLastPaymentNumberAsync,
                    cancellationToken),
                StudentId = fee.StudentId,
                StudentFeeId = fee.Id,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate.Date,
                PaymentMethod = request.PaymentMethod,
                Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                ReceivedByUserId = userId,
                Status = PaymentStatus.Active,
                CreatedAt = now,
                Receipt = new Receipt
                {
                    ReceiptNumber = await GenerateNumberAsync(
                        DocumentCodes.Receipt,
                        request.PaymentDate.Year,
                        _unitOfWork.Receipts.GetLastReceiptNumberAsync,
                        cancellationToken),
                    IssueDate = now,
                    IssuedByUserId = userId,
                    PrintCount = 0,
                    CreatedAt = now
                }
            };

            fee.PaidAmount += request.Amount;
            fee.Status = Services.Internal.FeeStatusCalculator.ForFee(fee);
            fee.UpdatedAt = now;

            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);

            await _auditService.RecordAsync(
                AuditAction.PaymentRegistered,
                nameof(Payment),
                payment.PaymentNumber,
                $"Payment {payment.PaymentNumber} of {Money.Format(payment.Amount)} received from "
                    + $"'{fee.Student.FullName}' ({fee.Student.StudentNumber}) for "
                    + $"{Period.FeeLabel(fee.PaymentType.Name, fee.Month, fee.Year)} by {request.PaymentMethod}.",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Payment {PaymentNumber} of {Amount} registered on student fee {StudentFeeId}",
                payment.PaymentNumber,
                payment.Amount,
                fee.Id);

            return new RegisterPaymentResult(
                payment.Id,
                payment.PaymentNumber,
                payment.Receipt.Id,
                payment.Receipt.ReceiptNumber,
                payment.Amount,
                fee.RemainingAmount);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RegisterMultiPaymentResult> RegisterManyAsync(
        RegisterMultiPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.RegisterPayments);

        if (request.Allocations is null || request.Allocations.Count == 0)
        {
            throw new DomainException("Select at least one obligation to settle.");
        }

        if (request.Allocations.Any(line => line.Amount <= 0))
        {
            throw new DomainException("Each allocation amount must be greater than zero.");
        }

        if (request.Allocations.Select(line => line.StudentFeeId).Distinct().Count() != request.Allocations.Count)
        {
            throw new DomainException("The same obligation cannot be selected twice.");
        }

        var results = new List<RegisterPaymentResult>();
        RegisterPaymentResult? first = null;

        foreach (var line in request.Allocations)
        {
            var single = new RegisterPaymentRequest(
                request.StudentId,
                line.StudentFeeId,
                line.Amount,
                request.PaymentDate,
                request.PaymentMethod,
                request.Reference,
                request.Notes,
                request.DuplicateConfirmed);

            var result = await RegisterAsync(single, cancellationToken);
            results.Add(result);
            first ??= result;
        }

        return new RegisterMultiPaymentResult(
            results,
            results.Sum(item => item.AmountApplied),
            first?.ReceiptId,
            first?.ReceiptNumber);
    }

    public async Task CancelAsync(CancelPaymentRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.CancelPayments);
        RequestValidation.Ensure(request, new CancelPaymentRequestValidator());

        var payment = await _unitOfWork.Payments.Query(trackChanges: true)
            .Include(entity => entity.Student)
            .Include(entity => entity.StudentFee)
                .ThenInclude(fee => fee.PaymentType)
            .FirstOrDefaultAsync(entity => entity.Id == request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        if (payment.Status != PaymentStatus.Active)
        {
            throw new DomainException($"Payment {payment.PaymentNumber} is already {payment.Status}.");
        }

        if (payment.Student.SchoolYearId is int schoolYearId)
        {
            await _schoolYearService.EnsureOpenForTransactionsAsync(schoolYearId, cancellationToken);
        }

        var now = DateTime.Now;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var fee = payment.StudentFee;
            fee.PaidAmount -= payment.Amount;
            if (fee.PaidAmount < 0)
            {
                fee.PaidAmount = 0m;
            }

            fee.Status = fee.Status == FeeStatus.Cancelled
                ? FeeStatus.Cancelled
                : Services.Internal.FeeStatusCalculator.ForFee(fee);
            fee.UpdatedAt = now;

            payment.Status = request.Reverse ? PaymentStatus.Reversed : PaymentStatus.Cancelled;
            payment.CancelledAt = now;
            payment.CancelledByUserId = _currentUser.RequireUserId();
            payment.CancellationReason = request.Reason.Trim();

            await _auditService.RecordAsync(
                request.Reverse ? AuditAction.PaymentReversed : AuditAction.PaymentCancelled,
                nameof(Payment),
                payment.PaymentNumber,
                $"Payment {payment.PaymentNumber} of {Money.Format(payment.Amount)} "
                    + $"({payment.Student.FullName}) {(request.Reverse ? "reversed" : "cancelled")}. "
                    + $"Reason: {request.Reason.Trim()}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        _logger.LogWarning(
            "Payment {PaymentNumber} {Status} by user {UserId}",
            payment.PaymentNumber,
            payment.Status,
            payment.CancelledByUserId);
    }

    private static async Task<string> GenerateNumberAsync(
        string documentCode,
        int year,
        Func<string, CancellationToken, Task<string?>> lastNumberLookup,
        CancellationToken cancellationToken)
    {
        var prefix = DocumentNumber.BuildPrefix(documentCode, year);
        var last = await lastNumberLookup(prefix, cancellationToken);
        return DocumentNumber.Next(documentCode, year, last);
    }
}
