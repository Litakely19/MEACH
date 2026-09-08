using System.Reflection;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Authentication;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels;

/// <summary>
/// Credential screen. A session only starts once the password has been accepted
/// and, for a seeded account, replaced.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _schoolName = "School Management";

    public LoginViewModel(
        ILogger<LoginViewModel> logger,
        IScopedExecutor scopedExecutor,
        ICurrentUserService currentUser,
        IDialogService dialogService,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _currentUser = currentUser;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;

        LoginCommand = new AsyncRelayCommand(SignInAsync, () => !IsBusy);
    }

    /// <summary>Raised once the operator is authenticated and allowed into the shell.</summary>
    public event EventHandler? SignedIn;

    public ICommand LoginCommand { get; }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string SchoolName
    {
        get => _schoolName;
        private set => SetProperty(ref _schoolName, value);
    }

    public string VersionLabel { get; } =
        $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";

    public override async Task LoadAsync()
    {
        // The school name is read before authentication so the window carries the
        // establishment identity rather than a generic product title.
        await RunGuardedAsync(async () =>
        {
            var settings = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<ISchoolSettingsService>().GetAsync());

            SchoolName = settings.SchoolName;
        });
    }

    private async Task SignInAsync()
    {
        var succeeded = await RunGuardedAsync(async () =>
        {
            var result = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAuthenticationService>()
                    .LoginAsync(new LoginRequest(Username, Password)));

            if (!result.Succeeded || result.User is null)
            {
                ErrorMessage = result.ErrorMessage ?? "Sign in failed.";
                return false;
            }

            return true;
        });

        if (!succeeded)
        {
            return;
        }

        Password = string.Empty;

        if (_currentUser.User?.MustChangePassword == true && !await ForcePasswordChangeAsync())
        {
            return;
        }

        SignedIn?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Blocks the session until the seeded password is replaced; declining signs the
    /// operator out again.
    /// </summary>
    private async Task<bool> ForcePasswordChangeAsync()
    {
        var user = _currentUser.User!;
        var viewModel = _serviceProvider.GetRequiredService<ChangePasswordViewModel>();
        viewModel.Initialize(user.Id, user.Username, isMandatory: true);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            return true;
        }

        await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IAuthenticationService>().LogoutAsync());

        ErrorMessage = "The default password must be changed before using the application.";
        return false;
    }
}
