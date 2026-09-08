using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Users;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

/// <summary>
/// Administrator reset of another account's password. The current password is not
/// asked for, so the new one is normally temporary and must be changed at the next
/// sign in.
/// </summary>
public class ResetPasswordViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int _userId;
    private string _username = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _requireChangeAtNextLogin = true;

    public ResetPasswordViewModel(ILogger<ResetPasswordViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        Title = "Reset password";
        ConfirmButtonText = "Reset password";
        DialogWidth = 460;
    }

    public string PolicyHint => PasswordPolicy.Describe();

    public string Username
    {
        get => _username;
        private set => SetProperty(ref _username, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public bool RequireChangeAtNextLogin
    {
        get => _requireChangeAtNextLogin;
        set => SetProperty(ref _requireChangeAtNextLogin, value);
    }

    public void Initialize(int userId, string username)
    {
        _userId = userId;
        Username = username;
        Title = $"Reset the password of {username}";
    }

    protected override async Task<bool> SaveAsync()
    {
        if (!PasswordPolicy.IsValid(NewPassword, out var policyError))
        {
            ErrorMessage = policyError;
            return false;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "The password and its confirmation do not match.";
            return false;
        }

        await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IUserService>().ResetPasswordAsync(
                new ResetUserPasswordRequest(_userId, NewPassword, RequireChangeAtNextLogin)));

        return true;
    }
}
