namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Single row holding the school identity printed on invoices and receipts.
/// </summary>
public class SchoolSetting : BaseEntity
{
    public string SchoolName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string CurrencyCode { get; set; } = "MGA";

    public string CurrencySymbol { get; set; } = "Ar";

    public string? ReceiptFooter { get; set; }

    public string? LogoPath { get; set; }

    /// <summary>Day of month used as the default due date when generating monthly fees.</summary>
    public int DefaultDueDay { get; set; } = 10;

    public DateTime? UpdatedAt { get; set; }
}
