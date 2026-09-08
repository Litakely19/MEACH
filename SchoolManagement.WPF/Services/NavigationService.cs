using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.Services;

/// <summary>
/// Resolves a fresh view model for every navigation and loads it before it becomes
/// the active screen, so returning to a list always shows current data.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase? _current;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ViewModelBase? Current
    {
        get => _current;
        private set
        {
            _current = value;
            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CurrentChanged;

    public Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase =>
        NavigateToAsync<TViewModel>(_ => { });

    public async Task NavigateToAsync(Type viewModelType, Action<ViewModelBase>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        if (!typeof(ViewModelBase).IsAssignableFrom(viewModelType))
        {
            throw new ArgumentException($"{viewModelType.Name} is not a screen view model.", nameof(viewModelType));
        }

        var viewModel = (ViewModelBase)_serviceProvider.GetRequiredService(viewModelType);
        configure?.Invoke(viewModel);

        Current = viewModel;
        await viewModel.LoadAsync();
    }

    public async Task NavigateToAsync<TViewModel>(Action<TViewModel> configure) where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        configure(viewModel);

        Current = viewModel;
        await viewModel.LoadAsync();
    }

    public void Reset() => Current = null;
}
