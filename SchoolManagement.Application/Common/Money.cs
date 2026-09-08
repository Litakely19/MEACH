using System.Globalization;

namespace SchoolManagement.Application.Common;

/// <summary>
/// Formatting for Malagasy Ariary amounts, shared by the UI, PDF documents and
/// Excel exports so that every screen and printout reads the same way.
/// </summary>
public static class Money
{
    public const string DefaultSymbol = "Ar";

    private static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 0
    };

    /// <summary>Formats an amount without its currency symbol, for example "1 250 000".</summary>
    public static string FormatAmount(decimal amount) =>
        amount.ToString(HasFraction(amount) ? "N2" : "N0", NumberFormat);

    /// <summary>Formats an amount with its currency symbol, for example "1 250 000 Ar".</summary>
    public static string Format(decimal amount, string? symbol = null) =>
        $"{FormatAmount(amount)} {(string.IsNullOrWhiteSpace(symbol) ? DefaultSymbol : symbol)}";

    private static bool HasFraction(decimal amount) => amount != decimal.Truncate(amount);
}
