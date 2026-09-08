using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Users;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

/// <summary>
/// Creates a staff account or edits an existing one. The password is only set here
/// when the account is created; afterwards it is reset from the user list.
/// </summary>
public class UserEditViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int? _userId;
    private int? _initialRoleId;
    private string _username = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string? _email;
    private RoleOption? _selectedRole;
    private bool _isActive = true;
    private bool _mustChangePassword = true;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    public UserEditViewModel(ILogger<UserEditViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        DialogWidth = 520;
    }

    public ObservableCollection<RoleOption> Roles { get; } = new();

    public bool IsNew => _userId is null;

    public string PolicyHint => PasswordPolicy.Describe();

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public RoleOption? SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool MustChangePassword
    {
        get => _mustChangePassword;
        set => SetProperty(ref _mustChangePassword, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public void InitializeForCreate()
    {
        _userId = null;
        Title = "New user";
        ConfirmButtonText = "Create user";
        OnPropertyChanged(nameof(IsNew));
    }

    public void InitializeForEdit(UserListItem user)
    {
        _userId = user.Id;
        Username = user.Username;
        FirstName = user.FirstName;
        LastName = user.LastName;
        Email = user.Email;
        IsActive = user.IsActive;
        _initialRoleId = user.RoleId;

        Title = $"User {user.Username}";
        ConfirmButtonText = "Save";
        OnPropertyChanged(nameof(IsNew));
    }

    public override async Task LoadAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var roles = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IUserService>().ListRolesAsync());

            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }

            SelectedRole = _initialRoleId is null
                ? Roles.FirstOrDefault()
                : Roles.FirstOrDefault(role => role.Id == _initialRoleId);
        });
    }

    protected override async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "The user name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "The first name and the last name are required.";
            return false;
        }

        if (SelectedRole is null)
        {
            ErrorMessage = "Select a role.";
            return false;
        }

        if (IsNew)
        {
            if (!PasswordPolicy.IsValid(Password, out var policyError))
            {
                ErrorMessage = policyError;
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "The password and its confirmation do not match.";
                return false;
            }

            await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IUserService>().CreateAsync(new CreateUserRequest(
                    Username.Trim(),
                    Password,
                    FirstName.Trim(),
                    LastName.Trim(),
                    string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    SelectedRole.Id,
                    IsActive,
                    MustChangePassword)));

            return true;
        }

        await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IUserService>().UpdateAsync(new UpdateUserRequest(
                _userId!.Value,
                Username.Trim(),
                FirstName.Trim(),
                LastName.Trim(),
                string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                SelectedRole.Id,
                IsActive)));

        return true;
    }
}
