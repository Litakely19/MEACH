using System.Globalization;

namespace SchoolManagement.Application.Common;

/// <summary>
/// Helpers for the month/year pair that identifies a monthly scholar fee.
/// </summary>
public static class Period
{
    public static string FeeLabel(string paymentTypeName, int? month, int? year) =>
        month is null || year is null
            ? paymentTypeName
            : $"{paymentTypeName} – {Label(month, year)}";

    public static string Label(int? month, int? year)
    {
        if (month is null || year is null)
        {
            return "One-time";
        }

        if (month < 1 || month > 12)
        {
            return year.Value.ToString(CultureInfo.CurrentCulture);
        }

        var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month.Value);
        return $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(monthName)} {year.Value}";
    }

    public static string MonthName(int month) =>
        month is < 1 or > 12
            ? string.Empty
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month));

    /// <summary>
    /// Months covered by a school year, in academic order (September to June for a
    /// September start), derived from the year's own start and end dates.
    /// </summary>
    public static IReadOnlyList<(int Month, int Year)> EnumerateMonths(DateTime startDate, DateTime endDate)
    {
        var months = new List<(int Month, int Year)>();
        var cursor = new DateTime(startDate.Year, startDate.Month, 1);
        var last = new DateTime(endDate.Year, endDate.Month, 1);

        while (cursor <= last)
        {
            months.Add((cursor.Month, cursor.Year));
            cursor = cursor.AddMonths(1);
        }

        return months;
    }
}
