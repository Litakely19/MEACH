using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.PaymentTypes;

public sealed class PaymentTypeScope
{
    public static PaymentTypeScope All { get; } = new();

    public static PaymentTypeScope Monthly { get; } = new([PaymentFrequency.Monthly]);

    public static PaymentTypeScope Droit { get; } = Named(WellKnownPaymentTypes.Droit);

    public static PaymentTypeScope Ecolage { get; } = Named(WellKnownPaymentTypes.Ecolage);

    public static PaymentTypeScope Livre { get; } = Named(WellKnownPaymentTypes.Livre);

    public static PaymentTypeScope MockExam { get; } = Named(WellKnownPaymentTypes.MockExam);

    public static PaymentTypeScope OfficialExam { get; } = Named(WellKnownPaymentTypes.OfficialExam);

    public static PaymentTypeScope OneTime { get; } = new([PaymentFrequency.OneTime]);

    /// <summary>
    /// Custom types only (not Droit / Écolage / Livre / exams). Frequency is not
    /// restricted so types created with the wrong default still appear under Other fees.
    /// </summary>
    public static PaymentTypeScope CustomOneTime { get; } = new(
        excludedExactNames: WellKnownPaymentTypes.All);

    public static PaymentTypeScope Named(string exactName) => new(exactName: exactName);

    public static PaymentTypeScope For(PaymentFrequency frequency) => new([frequency]);

    private PaymentTypeScope(
        IReadOnlyList<PaymentFrequency>? frequencies = null,
        string? nameContains = null,
        string? nameDoesNotContain = null,
        string? exactName = null,
        IReadOnlyList<string>? excludedExactNames = null)
    {
        Frequencies = frequencies;
        NameContains = nameContains;
        NameDoesNotContain = nameDoesNotContain;
        ExactName = exactName;
        ExcludedExactNames = excludedExactNames;
    }

    public IReadOnlyList<PaymentFrequency>? Frequencies { get; }

    public string? NameContains { get; }

    public string? NameDoesNotContain { get; }

    public string? ExactName { get; }

    public IReadOnlyList<string>? ExcludedExactNames { get; }

    public bool Matches(string name, PaymentFrequency frequency)
    {
        if (!string.IsNullOrWhiteSpace(ExactName)
            && !string.Equals(name, ExactName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Frequencies is { Count: > 0 } && !Frequencies.Contains(frequency))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(NameContains)
            && name.IndexOf(NameContains, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(NameDoesNotContain)
            && name.IndexOf(NameDoesNotContain, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (ExcludedExactNames is { Count: > 0 }
            && ExcludedExactNames.Any(excluded =>
                string.Equals(excluded, name, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}
