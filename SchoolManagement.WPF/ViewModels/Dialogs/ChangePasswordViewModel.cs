using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Authentication;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class ChangePasswordViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int _userId;
    private string _username = string.Empty;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    public ChangePasswordViewModel(ILogger<ChangePasswordViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        Title = "Change password";
        ConfirmButtonText = "Change password";
        DialogWidth = 460;
    }

    public string PolicyHint => PasswordPolicy.Describe();

    /// <summary>True when the account still carries the seeded password.</summary>
    public bool IsMandatory { get; private set; }

    public string Username
    {
        get => _username;
        private set => SetProperty(ref _username, value);
    }

    public string CurrentPassword
    {
        get => _currentPassword;
        set => SetProperty(ref _currentPassword, value);
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

    public void Initialize(int userId, string username, bool isMandatory)
    {
        _userId = userId;
        Username = username;
        IsMandatory = isMandatory;

        Title = isMandatory ? "Choose a new password" : "Change password";
        StatusMessage = isMandatory
            ? "This account still uses the default password. Choose a new one to continue."
            : null;
    }

    protected override async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(CurrentPassword))
        {
            ErrorMessage = "Enter your current password.";
            return false;
        }

        if (!PasswordPolicy.IsValid(NewPassword, out var policyError))
        {
            ErrorMessage = policyError;
            return false;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "The new password and its confirmation do not match.";
            return false;
        }

        await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IAuthenticationService>().ChangePasswordAsync(
                new ChangePasswordRequest(_userId, CurrentPassword, NewPassword, ConfirmPassword)));

        return true;
    }
}
