using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class PaymentTypeService : IPaymentTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public PaymentTypeService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<PaymentTypeListItem>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PaymentTypes.Query();

        if (!includeInactive)
        {
            query = query.Where(type => type.IsActive);
        }

        var types = await query.OrderBy(type => type.Name).ToListAsync(cancellationToken);

        return types
            .Select(type => new PaymentTypeListItem(
                type.Id,
                type.Name,
                type.Description,
                type.DefaultAmount,
                type.Frequency,
                type.IsActive))
            .ToList();
    }

    public Task<IReadOnlyList<PaymentTypeOption>> ListOptionsAsync(
        PaymentFrequency? frequency = null,
        CancellationToken cancellationToken = default) =>
        ListOptionsAsync(
            frequency is null ? PaymentTypeScope.All : PaymentTypeScope.For(frequency.Value),
            cancellationToken);

    public async Task<IReadOnlyList<PaymentTypeOption>> ListOptionsAsync(
        PaymentTypeScope scope,
        CancellationToken cancellationToken = default)
    {
        var types = await _unitOfWork.PaymentTypes.ListActiveAsync(cancellationToken: cancellationToken);

        return types
            .Where(type => scope.Matches(type.Name, type.Frequency))
            .Select(type => new PaymentTypeOption(type.Id, type.Name, type.DefaultAmount, type.Frequency))
            .ToList();
    }

    public async Task<int> CreateAsync(CreatePaymentTypeRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManagePaymentTypes);
        RequestValidation.Ensure(request, new CreatePaymentTypeRequestValidator());

        var name = request.Name.Trim();
        if (await _unitOfWork.PaymentTypes.GetByNameAsync(name, cancellationToken) is not null)
        {
            throw new DomainException($"A payment type named '{name}' already exists.");
        }

        var paymentType = new PaymentType
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DefaultAmount = request.DefaultAmount,
            Frequency = request.Frequency,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.PaymentTypes.AddAsync(paymentType, cancellationToken);
        await _auditService.RecordAsync(
            AuditAction.Created,
            nameof(PaymentType),
            null,
            $"Payment type '{name}' created.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return paymentType.Id;
    }

    public async Task UpdateAsync(UpdatePaymentTypeRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManagePaymentTypes);

        var paymentType = await _unitOfWork.PaymentTypes.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentType), request.Id);

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("The payment type name is required.");
        }

        if (request.DefaultAmount < 0)
        {
            throw new DomainException("The default amount cannot be negative.");
        }

        var duplicate = await _unitOfWork.PaymentTypes.Query()
            .AnyAsync(type => type.Name == name && type.Id != request.Id, cancellationToken);

        if (duplicate)
        {
            throw new DomainException($"A payment type named '{name}' already exists.");
        }

        paymentType.Name = name;
        paymentType.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        paymentType.DefaultAmount = request.DefaultAmount;
        paymentType.Frequency = request.Frequency;
        paymentType.IsActive = request.IsActive;
        paymentType.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(PaymentType),
            paymentType.Id,
            $"Payment type '{name}' updated.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(int paymentTypeId, bool isActive, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManagePaymentTypes);

        var paymentType = await _unitOfWork.PaymentTypes.GetByIdAsync(paymentTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentType), paymentTypeId);

        paymentType.IsActive = isActive;
        paymentType.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(PaymentType),
            paymentType.Id,
            $"Payment type '{paymentType.Name}' {(isActive ? "activated" : "deactivated")}.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
