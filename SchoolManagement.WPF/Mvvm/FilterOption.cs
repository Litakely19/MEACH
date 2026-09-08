using SchoolManagement.WPF.Converters;

namespace SchoolManagement.WPF.Mvvm;

/// <summary>
/// Entry of a filter combo box, where the absence of a value means "no filter".
/// </summary>
public record FilterOption<T>(T? Value, string Label) where T : struct
{
    public override string ToString() => Label;
}

public static class FilterOption
{
    /// <summary>Builds the "all" entry followed by one entry per enumeration member.</summary>
    public static IReadOnlyList<FilterOption<TEnum>> ForEnum<TEnum>(string allLabel)
        where TEnum : struct, Enum
    {
        var options = new List<FilterOption<TEnum>> { new(null, allLabel) };

        options.AddRange(Enum.GetValues<TEnum>()
            .Select(value => new FilterOption<TEnum>(value, EnumDisplayConverter.Humanize(value.ToString()!))));

        return options;
    }

    public static IReadOnlyList<FilterOption<int>> ForItems<TSource>(
        string allLabel,
        IEnumerable<TSource> source,
        Func<TSource, int> idSelector,
        Func<TSource, string> labelSelector)
    {
        var options = new List<FilterOption<int>> { new(null, allLabel) };

        options.AddRange(source.Select(item => new FilterOption<int>(idSelector(item), labelSelector(item))));

        return options;
    }
}
