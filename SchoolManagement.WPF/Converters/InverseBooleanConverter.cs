using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchoolManagement.WPF.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool boolean && !boolean;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool boolean && !boolean;
}

/// <summary>Collapses an element when the bound value is null or an empty string.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEmpty = value is null || (value is string text && string.IsNullOrWhiteSpace(text));
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);

        return isEmpty ^ invert ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// Shows the "nothing to display" placeholder when a count is zero. Pass "invert"
/// to show the element only when the list has rows.
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            _ => 0
        };

        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);

        return count == 0 ^ invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
