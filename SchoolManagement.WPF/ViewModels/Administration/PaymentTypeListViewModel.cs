using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.Administration;

/// <summary>
/// Catalogue of billable items. A payment type already used by an invoice is
/// deactivated rather than removed, so historical invoices keep their label.
/// </summary>
public class PaymentTypeListViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private PaymentTypeListItem? _selectedItem;
    private bool _includeInactive = true;

    public PaymentTypeListViewModel(
        ILogger<PaymentTypeListViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        CreateCommand = new AsyncRelayCommand(CreateAsync, () => CanManage);
        EditCommand = new AsyncRelayCommand(EditAsync, () => CanManage && SelectedItem is not null);
        ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync, () => CanManage && SelectedItem is not null);
    }

    public ObservableCollection<PaymentTypeListItem> PaymentTypes { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand CreateCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand ToggleActiveCommand { get; }

    public bool CanManage => _currentUser.HasPermission(Permission.ManagePaymentTypes);

    public bool IsEmpty => PaymentTypes.Count == 0;

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public PaymentTypeListItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(ToggleActiveLabel));
                (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ToggleActiveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ToggleActiveLabel => SelectedItem?.IsActive == false ? "Reactivate" : "Deactivate";

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var items = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IPaymentTypeService>().ListAsync(IncludeInactive));

            PaymentTypes.Clear();
            foreach (var item in items)
            {
                PaymentTypes.Add(item);
            }

            OnPropertyChanged(nameof(IsEmpty));
        });

    private async Task CreateAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<PaymentTypeEditViewModel>();
        viewModel.InitializeForCreate();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The payment type has been created.";
        }
    }

    private async Task EditAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<PaymentTypeEditViewModel>();
        viewModel.InitializeForEdit(SelectedItem);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The payment type has been updated.";
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var item = SelectedItem;
        var activate = !item.IsActive;

        var question = activate
            ? $"Make {item.Name} available again?"
            : $"Stop offering {item.Name}? Existing invoices keep it.";

        if (!_dialogService.Confirm(question, activate ? "Reactivate" : "Deactivate"))
        {
            return;
        }

        var changed = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentTypeService>().SetActiveAsync(item.Id, activate)));

        if (changed)
        {
            await LoadAsync();
        }
    }
}
