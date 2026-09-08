using System.Globalization;

namespace SchoolManagement.Domain.Common;

/// <summary>
/// Builds and reads the sequential document numbers used for students, invoices,
/// payments and receipts, in the form PREFIX-YEAR-SEQUENCE (for example INV-2026-0001).
/// </summary>
public static class DocumentNumber
{
    private const int SequenceDigits = 4;

    public static string BuildPrefix(string documentCode, int year) =>
        $"{documentCode}-{year}-";

    public static string Build(string documentCode, int year, int sequence) =>
        BuildPrefix(documentCode, year) + sequence.ToString(new string('0', SequenceDigits), CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the sequence encoded in <paramref name="documentNumber"/>, or 0 when
    /// the value does not follow the expected shape.
    /// </summary>
    public static int ExtractSequence(string? documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return 0;
        }

        var separatorIndex = documentNumber.LastIndexOf('-');
        if (separatorIndex < 0 || separatorIndex == documentNumber.Length - 1)
        {
            return 0;
        }

        var sequencePart = documentNumber[(separatorIndex + 1)..];
        return int.TryParse(sequencePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : 0;
    }

    public static string Next(string documentCode, int year, string? lastDocumentNumber) =>
        Build(documentCode, year, ExtractSequence(lastDocumentNumber) + 1);
}
