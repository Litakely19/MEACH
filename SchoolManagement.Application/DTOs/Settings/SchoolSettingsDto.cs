namespace SchoolManagement.Application.DTOs.Settings;

public record SchoolSettingsDto(
    int Id,
    string SchoolName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    string CurrencyCode,
    string CurrencySymbol,
    string? ReceiptFooter,
    string? LogoPath,
    int DefaultDueDay);

public record UpdateSchoolSettingsRequest(
    string SchoolName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    string CurrencyCode,
    string CurrencySymbol,
    string? ReceiptFooter,
    string? LogoPath,
    int DefaultDueDay);
