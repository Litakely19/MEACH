using System.Globalization;
using System.Windows.Data;
using SchoolManagement.Application.Common;

namespace SchoolManagement.WPF.Converters;

/// <summary>
/// Formats a monetary value the same way as the printed documents.
/// Pass "bare" as the parameter to leave the currency symbol out.
/// </summary>
public class MoneyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var amount = value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => (decimal)doubleValue,
            int intValue => intValue,
            long longValue => longValue,
            _ => 0m
        };

        return string.Equals(parameter as string, "bare", StringComparison.OrdinalIgnoreCase)
            ? Money.FormatAmount(amount)
            : Money.Format(amount);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
