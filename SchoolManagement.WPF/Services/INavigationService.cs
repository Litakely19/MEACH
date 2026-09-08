using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.Services;

public interface INavigationService
{
    ViewModelBase? Current { get; }

    event EventHandler? CurrentChanged;

    Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase;

    /// <summary>Navigates to a screen chosen at runtime, used by the side menu.</summary>
    Task NavigateToAsync(Type viewModelType, Action<ViewModelBase>? configure = null);

    /// <summary>
    /// Navigates and lets the caller prepare the target, used to open a screen on a
    /// specific record such as a student file.
    /// </summary>
    Task NavigateToAsync<TViewModel>(Action<TViewModel> configure) where TViewModel : ViewModelBase;

    void Reset();
}
