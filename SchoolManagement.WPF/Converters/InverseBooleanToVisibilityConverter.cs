using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchoolManagement.WPF.Converters;

/// <summary>
/// Shows an element when the bound flag is false, for the "disabled" and "not yet
/// done" badges that are the counterpart of a positive flag.
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
