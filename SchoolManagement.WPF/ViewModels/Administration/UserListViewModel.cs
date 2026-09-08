using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Users;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.Administration;

/// <summary>
/// Staff accounts: creation, role assignment, deactivation and password reset.
/// Accounts are never deleted, so the payments they received keep a valid author.
/// </summary>
public class UserListViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private string? _searchTerm;
    private UserListItem? _selectedUser;

    public UserListViewModel(
        ILogger<UserListViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        SearchCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        CreateCommand = new AsyncRelayCommand(CreateAsync, () => CanManage);
        EditCommand = new AsyncRelayCommand(EditAsync, () => CanManage && SelectedUser is not null);
        ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync, () => CanManage && SelectedUser is not null);
        ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync, CanToggleActive);
    }

    public ObservableCollection<UserListItem> Users { get; } = new();

    public ICommand SearchCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand CreateCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand ResetPasswordCommand { get; }

    public ICommand ToggleActiveCommand { get; }

    public bool CanManage => _currentUser.HasPermission(Permission.ManageUsers);

    public string? SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public UserListItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                OnPropertyChanged(nameof(ToggleActiveLabel));
                RaiseSelectionCommands();
            }
        }
    }

    public string ToggleActiveLabel => SelectedUser?.IsActive == false ? "Reactivate" : "Deactivate";

    public bool IsEmpty => Users.Count == 0;

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var users = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IUserService>().ListAsync(SearchTerm));

            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            OnPropertyChanged(nameof(IsEmpty));
            RaiseSelectionCommands();
        });

    private async Task CreateAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<UserEditViewModel>();
        viewModel.InitializeForCreate();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The user account has been created.";
        }
    }

    private async Task EditAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<UserEditViewModel>();
        viewModel.InitializeForEdit(SelectedUser);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The user account has been updated.";
        }
    }

    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<ResetPasswordViewModel>();
        viewModel.Initialize(SelectedUser.Id, SelectedUser.Username);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The password has been reset.";
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        var user = SelectedUser;
        var activate = !user.IsActive;

        var question = activate
            ? $"Reactivate the account of {user.FullName}?"
            : $"Deactivate the account of {user.FullName}? The user will no longer be able to sign in.";

        if (!_dialogService.Confirm(question, activate ? "Reactivate" : "Deactivate"))
        {
            return;
        }

        var changed = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IUserService>().SetActiveAsync(user.Id, activate)));

        if (changed)
        {
            await LoadAsync();
            StatusMessage = activate
                ? $"The account of {user.FullName} is active again."
                : $"The account of {user.FullName} has been deactivated.";
        }
    }

    private bool CanToggleActive() =>
        CanManage && SelectedUser is not null && SelectedUser.Id != _currentUser.User?.Id;

    private void RaiseSelectionCommands()
    {
        (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ResetPasswordCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ToggleActiveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
