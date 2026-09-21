using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Receipts;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ReceiptService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<PagedResult<ReceiptListItem>> ListAsync(
        ReceiptFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReceipts);

        var query = _unitOfWork.Receipts.Query();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(receipt => receipt.Payment.StudentId == filter.StudentId);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.Date;
            query = query.Where(receipt => receipt.IssueDate >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.Date.AddDays(1);
            query = query.Where(receipt => receipt.IssueDate < to);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var names = PersonNameSearch.Parse(filter.SearchTerm);
            var lower = names.Lower;
            var firstPart = names.FirstPart;
            var lastPart = names.LastPart;
            var hasTwoParts = names.HasTwoParts;

            query = query.Where(receipt =>
                receipt.ReceiptNumber.ToLower().Contains(lower)
                || receipt.Payment.PaymentNumber.ToLower().Contains(lower)
                || receipt.Payment.Student.StudentNumber.ToLower().Contains(lower)
                || receipt.Payment.Student.FirstName.ToLower().Contains(lower)
                || receipt.Payment.Student.LastName.ToLower().Contains(lower)
                || (receipt.Payment.Student.FirstName + " " + receipt.Payment.Student.LastName).ToLower().Contains(lower)
                || (receipt.Payment.Student.LastName + " " + receipt.Payment.Student.FirstName).ToLower().Contains(lower)
                || (hasTwoParts
                    && receipt.Payment.Student.FirstName.ToLower().Contains(firstPart)
                    && receipt.Payment.Student.LastName.ToLower().Contains(lastPart)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageSize = Math.Max(1, filter.PageSize);
        var page = Math.Max(1, filter.Page);

        var items = await query
            .OrderByDescending(receipt => receipt.IssueDate)
            .ThenByDescending(receipt => receipt.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(receipt => new ReceiptListItem(
                receipt.Id,
                receipt.ReceiptNumber,
                receipt.PaymentId,
                receipt.Payment.PaymentNumber,
                receipt.Payment.Student.StudentNumber,
                receipt.Payment.Student.FirstName,
                receipt.Payment.Student.LastName,
                receipt.Payment.Student.AcademicLevel.Name,
                receipt.Payment.Student.StudentGroup.Name,
                receipt.Payment.Amount,
                receipt.Payment.PaymentMethod,
                receipt.IssueDate,
                receipt.IssuedByUser.FirstName + " " + receipt.IssuedByUser.LastName,
                receipt.PrintCount,
                receipt.Payment.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReceiptListItem>(items, totalCount, page, pageSize);
    }

    public async Task<ReceiptDocument> GetDocumentAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReceipts);

        var receipt = await _unitOfWork.Receipts.GetDetailAsync(receiptId, cancellationToken)
            ?? throw new NotFoundException(nameof(Receipt), receiptId);

        return BuildDocument(receipt, isReprint: receipt.PrintCount > 0, await LoadSettingsAsync(cancellationToken));
    }

    public async Task<ReceiptDocument> GetDocumentByPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReceipts);

        var receipt = await _unitOfWork.Receipts.GetByPaymentIdAsync(paymentId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Receipt for payment", paymentId);

        return await GetDocumentAsync(receipt.Id, cancellationToken);
    }

    public async Task<ReceiptDocument> RegisterPrintAsync(
        int receiptId,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewReceipts);

        var tracked = await _unitOfWork.Receipts.GetByIdAsync(receiptId, cancellationToken)
            ?? throw new NotFoundException(nameof(Receipt), receiptId);

        var isReprint = tracked.PrintCount > 0;
        tracked.PrintCount++;
        tracked.LastPrintedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.ReceiptPrinted,
            nameof(Receipt),
            tracked.ReceiptNumber,
            $"Receipt {tracked.ReceiptNumber} {(isReprint ? "reprinted" : "printed")} (copy #{tracked.PrintCount}).",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var receipt = await _unitOfWork.Receipts.GetDetailAsync(receiptId, cancellationToken)
            ?? throw new NotFoundException(nameof(Receipt), receiptId);

        return BuildDocument(receipt, isReprint, await LoadSettingsAsync(cancellationToken));
    }

    private Task<SchoolSetting> LoadSettingsAsync(CancellationToken cancellationToken) =>
        _unitOfWork.SchoolSettings.GetSettingsAsync(cancellationToken: cancellationToken);

    private static ReceiptDocument BuildDocument(
        Receipt receipt,
        bool isReprint,
        SchoolSetting settings)
    {
        var fee = receipt.Payment.StudentFee;
        var student = receipt.Payment.Student;
        var period = Period.FeeLabel(fee.PaymentType.Name, fee.Month, fee.Year);

        var lines = new[]
        {
            new ReceiptLine(
                fee.PaymentType.Name,
                fee.Notes ?? period,
                period,
                receipt.Payment.Amount)
        };

        return new ReceiptDocument(
            settings.SchoolName,
            settings.Address,
            settings.PhoneNumber,
            settings.Email,
            settings.LogoPath,
            receipt.ReceiptNumber,
            receipt.IssueDate,
            receipt.Payment.PaymentNumber,
            student.FullName,
            student.StudentNumber,
            student.AcademicLevel.Name,
            student.StudentGroup.Name,
            lines,
            receipt.Payment.Amount,
            receipt.Payment.PaymentMethod,
            receipt.Payment.Reference,
            receipt.Payment.PaymentDate,
            receipt.Payment.ReceivedByUser.FullName,
            fee.ExpectedAmount,
            fee.PaidAmount,
            fee.RemainingAmount,
            settings.CurrencySymbol,
            settings.ReceiptFooter,
            isReprint,
            receipt.PrintCount,
            receipt.Payment.Status);
    }
}
