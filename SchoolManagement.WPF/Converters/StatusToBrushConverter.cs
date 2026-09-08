using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.WPF.Converters;

/// <summary>
/// Colour coding shared by every list: green when settled, amber when partially
/// settled, red when late or cancelled.
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Paid = Frozen("#1E7F4F");
    private static readonly SolidColorBrush Partial = Frozen("#B26A00");
    private static readonly SolidColorBrush Overdue = Frozen("#C62828");
    private static readonly SolidColorBrush Pending = Frozen("#3C4A5A");
    private static readonly SolidColorBrush Cancelled = Frozen("#8A94A6");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        FeeStatus.Paid => Paid,
        FeeStatus.PartiallyPaid => Partial,
        FeeStatus.Overdue => Overdue,
        FeeStatus.Cancelled => Cancelled,
        FeeStatus.Unpaid => Pending,
        PaymentStatus.Active => Paid,
        PaymentStatus.Cancelled => Cancelled,
        PaymentStatus.Reversed => Overdue,
        StudentStatus.Active => Paid,
        StudentStatus.Suspended => Overdue,
        StudentStatus.Inactive => Cancelled,
        StudentStatus.Graduated => Pending,
        StudentStatus.Transferred => Partial,
        _ => Pending
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static SolidColorBrush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
