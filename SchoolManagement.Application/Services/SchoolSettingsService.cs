using SchoolManagement.Application.DTOs.Settings;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class SchoolSettingsService : ISchoolSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public SchoolSettingsService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<SchoolSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.SchoolSettings.GetSettingsAsync(cancellationToken: cancellationToken);

        return new SchoolSettingsDto(
            settings.Id,
            settings.SchoolName,
            settings.Address,
            settings.PhoneNumber,
            settings.Email,
            settings.Website,
            settings.CurrencyCode,
            settings.CurrencySymbol,
            settings.ReceiptFooter,
            settings.LogoPath,
            settings.DefaultDueDay);
    }

    public async Task UpdateAsync(
        UpdateSchoolSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSettings);

        if (string.IsNullOrWhiteSpace(request.SchoolName))
        {
            throw new DomainException("The school name is required.");
        }

        if (request.DefaultDueDay is < 1 or > 28)
        {
            throw new DomainException("The default due day must be between 1 and 28.");
        }

        var settings = await _unitOfWork.SchoolSettings.GetSettingsAsync(
            trackChanges: true,
            cancellationToken: cancellationToken);

        var isNew = settings.Id == 0;

        settings.SchoolName = request.SchoolName.Trim();
        settings.Address = Normalize(request.Address);
        settings.PhoneNumber = Normalize(request.PhoneNumber);
        settings.Email = Normalize(request.Email);
        settings.Website = Normalize(request.Website);
        settings.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "MGA" : request.CurrencyCode.Trim();
        settings.CurrencySymbol = string.IsNullOrWhiteSpace(request.CurrencySymbol) ? "Ar" : request.CurrencySymbol.Trim();
        settings.ReceiptFooter = Normalize(request.ReceiptFooter);
        settings.LogoPath = Normalize(request.LogoPath);
        settings.DefaultDueDay = request.DefaultDueDay;
        settings.UpdatedAt = DateTime.Now;

        if (isNew)
        {
            await _unitOfWork.SchoolSettings.AddAsync(settings, cancellationToken);
        }

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(SchoolSetting),
            settings.Id,
            "School settings updated.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
