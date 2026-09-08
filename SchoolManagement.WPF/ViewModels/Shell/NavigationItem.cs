using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.ViewModels.Shell;

/// <summary>
/// One entry of the side navigation. The item only describes the destination; the
/// shell performs the navigation, and <see cref="RequiredPermission"/> decides
/// whether the entry is offered to the signed in role at all.
/// </summary>
public class NavigationItem : ObservableObject
{
    private bool _isSelected;

    public NavigationItem(
        string title,
        string group,
        Type viewModelType,
        Permission requiredPermission,
        Action<ViewModelBase>? configure = null)
    {
        Title = title;
        Group = group;
        ViewModelType = viewModelType;
        RequiredPermission = requiredPermission;
        Configure = configure;
    }

    public string Title { get; }

    /// <summary>Heading the entry is listed under, empty for the top level entries.</summary>
    public string Group { get; }

    public Type ViewModelType { get; }

    public Permission RequiredPermission { get; }

    /// <summary>Optional setup run after the screen is resolved (e.g. custom payment type name).</summary>
    public Action<ViewModelBase>? Configure { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
