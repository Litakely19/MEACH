namespace SchoolManagement.Application.Common;

/// <summary>
/// Parses a person search box so "First Last" (and Last First) match across name fields.
/// Use the returned tokens in EF Where clauses — custom methods are not translated by EF.
/// </summary>
public static class PersonNameSearch
{
    public readonly record struct Terms(string Lower, string FirstPart, string LastPart, bool HasTwoParts);

    public static Terms Parse(string searchTerm)
    {
        var term = searchTerm.Trim();
        var lower = term.ToLowerInvariant();
        var parts = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new Terms(
            lower,
            parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty,
            parts.Length > 1 ? parts[^1].ToLowerInvariant() : string.Empty,
            parts.Length >= 2);
    }
}
