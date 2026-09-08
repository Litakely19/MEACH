using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SchoolManagement.Infrastructure.Data.Converters;

/// <summary>
/// Stores monetary <see cref="decimal"/> values as whole minor units (INTEGER).
/// </summary>
/// <remarks>
/// SQLite has no decimal type, so EF Core would otherwise persist money as TEXT.
/// Text columns compare lexicographically, which makes predicates such as
/// "TotalAmount &gt; PaidAmount" silently wrong ("100.00" sorts before "20.00").
/// Scaling to a 64 bit integer keeps every amount exact and correctly comparable
/// and sortable inside SQL, and fixes the scale at two decimal places.
/// </remarks>
public sealed class MoneyConverter : ValueConverter<decimal, long>
{
    public const int Scale = 100;

    public MoneyConverter()
        : base(
            amount => (long)decimal.Round(amount * Scale, 0, MidpointRounding.AwayFromZero),
            minorUnits => minorUnits / (decimal)Scale)
    {
    }
}
