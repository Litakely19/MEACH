using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace SchoolManagement.WPF.Converters;

/// <summary>
/// Turns an enumeration member into a readable label: "PartiallyPaid" reads
/// "Partially paid", "BankTransfer" reads "Bank transfer".
/// </summary>
public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? string.Empty : Humanize(value.ToString()!);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    public static string Humanize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(name.Length + 4);

        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];

            if (index > 0 && char.IsUpper(character) && !char.IsUpper(name[index - 1]))
            {
                builder.Append(' ');
                builder.Append(char.ToLower(character, CultureInfo.CurrentCulture));
                continue;
            }

            builder.Append(index == 0 ? char.ToUpper(character, CultureInfo.CurrentCulture) : character);
        }

        return builder.ToString();
    }
}
