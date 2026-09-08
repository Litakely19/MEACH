using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.SchoolYears;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.People;

/// <summary>
/// School years, the accounting periods of the school. Closing a year freezes its
/// invoices and payments; only an administrator can reopen it.
/// </summary>
public class SchoolYearListViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private SchoolYearListItem? _selectedYear;

    public SchoolYearListViewModel(
        ILogger<SchoolYearListViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        CreateCommand = new AsyncRelayCommand(CreateAsync, () => CanManage);
        EditCommand = new AsyncRelayCommand(EditAsync, () => CanManage && SelectedYear is not null);
        SetCurrentCommand = new AsyncRelayCommand(
            SetCurrentAsync,
            () => CanManage && SelectedYear is { IsCurrent: false, IsClosed: false });
        CloseYearCommand = new AsyncRelayCommand(
            CloseYearAsync,
            () => CanManage && SelectedYear is { IsClosed: false });
        ReopenYearCommand = new AsyncRelayCommand(
            ReopenYearAsync,
            () => CanManage && SelectedYear is { IsClosed: true });
    }

    public bool CanManage => _currentUser.HasPermission(Permission.ManageSchoolYears);

    public ObservableCollection<SchoolYearListItem> SchoolYears { get; } = new();

    public ICommand CreateCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand SetCurrentCommand { get; }

    public ICommand CloseYearCommand { get; }

    public ICommand ReopenYearCommand { get; }

    public SchoolYearListItem? SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value))
            {
                RaiseCommands();
            }
        }
    }

    public override Task LoadAsync() => LoadYearsAsync();

    private Task LoadYearsAsync() =>
        RunGuardedAsync(async () =>
        {
            var years = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<ISchoolYearService>().ListAsync());

            var previousId = SelectedYear?.Id;

            SchoolYears.Clear();
            foreach (var year in years)
            {
                SchoolYears.Add(year);
            }

            SelectedYear = SchoolYears.FirstOrDefault(year => year.Id == previousId)
                ?? SchoolYears.FirstOrDefault(year => year.IsCurrent)
                ?? SchoolYears.FirstOrDefault();
        });

    private async Task CreateAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<SchoolYearEditViewModel>();
        viewModel.InitializeForCreate();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadYearsAsync();
            StatusMessage = "The school year has been created.";
        }
    }

    private async Task EditAsync()
    {
        if (SelectedYear is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<SchoolYearEditViewModel>();
        viewModel.InitializeForEdit(SelectedYear);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadYearsAsync();
            StatusMessage = "The school year has been updated.";
        }
    }

    private async Task SetCurrentAsync()
    {
        if (SelectedYear is null)
        {
            return;
        }

        var year = SelectedYear;

        if (!_dialogService.Confirm(
                $"Make {year.Name} the current school year?\n\nNew enrollments and invoices will use it by default.",
                "Current school year"))
        {
            return;
        }

        var applied = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<ISchoolYearService>().SetCurrentAsync(year.Id)));

        if (applied)
        {
            await LoadYearsAsync();
            StatusMessage = $"{year.Name} is now the current school year.";
        }
    }

    private async Task CloseYearAsync()
    {
        if (SelectedYear is null)
        {
            return;
        }

        var year = SelectedYear;

        if (!_dialogService.ConfirmCritical(
                $"Close school year {year.Name}?\n\nNo invoice and no payment can be registered on a closed year.",
                "Close a school year"))
        {
            return;
        }

        var applied = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<ISchoolYearService>().CloseAsync(year.Id)));

        if (applied)
        {
            await LoadYearsAsync();
            StatusMessage = $"School year {year.Name} is closed.";
        }
    }

    private async Task ReopenYearAsync()
    {
        if (SelectedYear is null)
        {
            return;
        }

        var year = SelectedYear;

        if (!_dialogService.ConfirmCritical(
                $"Reopen school year {year.Name}?\n\nFinancial operations will be allowed again on this year.",
                "Reopen a school year"))
        {
            return;
        }

        var applied = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<ISchoolYearService>().ReopenAsync(year.Id)));

        if (applied)
        {
            await LoadYearsAsync();
            StatusMessage = $"School year {year.Name} is open again.";
        }
    }

    private void RaiseCommands()
    {
        (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (SetCurrentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CloseYearCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ReopenYearCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
