using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.DTOs.Students;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.People;

public record StudentBillMenuItem(string Key, string Label, PaymentFrequency Frequency, bool IsWellKnown);

public class StudentListViewModel : PagedListViewModel<StudentListItem>
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private FilterOption<int>? _selectedLevel;
    private FilterOption<int>? _selectedClass;
    private FilterOption<StudentStatus>? _selectedStatus;

    public StudentListViewModel(
        ILogger<StudentListViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        StatusOptions = FilterOption.ForEnum<StudentStatus>("All statuses");
        _selectedStatus = StatusOptions[0];

        CreateCommand = new AsyncRelayCommand(CreateAsync, () => CanManageStudents);
        EditCommand = new AsyncRelayCommand(EditAsync, () => CanManageStudents && SelectedItem is not null);
        OpenDetailCommand = new AsyncRelayCommand(OpenDetailAsync, () => SelectedItem is not null);
        TransferCommand = new AsyncRelayCommand(TransferAsync, () => CanManageStudents && SelectedItem is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => CanManageStudents && SelectedItem is not null);
        BillCommand = new AsyncRelayCommand(
            BillAsync,
            parameter => CanManageFees && SelectedItem is not null && parameter is StudentBillMenuItem);
        CombinedBillAndPayCommand = new AsyncRelayCommand(
            CombinedBillAndPayAsync,
            () => CanManageFees && SelectedItem is not null);
    }

    public bool CanManageStudents => _currentUser.HasPermission(Permission.ManageStudents);

    public bool CanManageFees => _currentUser.HasPermission(Permission.ManageFees);

    public ObservableCollection<FilterOption<int>> LevelOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> ClassOptions { get; } = new();

    public ObservableCollection<StudentBillMenuItem> BillMenuItems { get; } = new();

    public IReadOnlyList<FilterOption<StudentStatus>> StatusOptions { get; }

    public ICommand CreateCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand OpenDetailCommand { get; }

    public ICommand TransferCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand BillCommand { get; }

    public ICommand CombinedBillAndPayCommand { get; }

    public FilterOption<int>? SelectedLevel
    {
        get => _selectedLevel;
        set => SetFilter(ref _selectedLevel, value);
    }

    public FilterOption<int>? SelectedClass
    {
        get => _selectedClass;
        set => SetFilter(ref _selectedClass, value);
    }

    public FilterOption<StudentStatus>? SelectedStatus
    {
        get => _selectedStatus;
        set => SetFilter(ref _selectedStatus, value);
    }

    public override async Task LoadAsync()
    {
        await LoadLookupsAsync();
        await base.LoadAsync();
    }

    protected override Task<PagedResult<StudentListItem>> FetchAsync(int page) =>
        _scopedExecutor.RunAsync(provider => provider.GetRequiredService<IStudentService>().ListAsync(
            new StudentFilter(
                SearchTerm,
                AcademicLevelId: SelectedLevel?.Value,
                StudentGroupId: SelectedClass?.Value,
                Status: SelectedStatus?.Value,
                Page: page,
                PageSize: PageSize)));

    protected override Task OnPageLoadedAsync()
    {
        RaiseSelectionCommands();
        return Task.CompletedTask;
    }

    private async Task LoadLookupsAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var (levels, classes, paymentTypes) = await _scopedExecutor.RunAsync(async provider =>
            {
                var levelOptions = await provider.GetRequiredService<IAcademicLevelService>()
                    .ListOptionsAsync(onlyActive: false);
                var classOptions = await provider.GetRequiredService<IStudentGroupService>()
                    .ListOptionsAsync(onlyActive: false);
                var typeOptions = CanManageFees
                    ? await provider.GetRequiredService<IPaymentTypeService>().ListOptionsAsync()
                    : Array.Empty<PaymentTypeOption>();

                return (levelOptions, classOptions, typeOptions);
            });

            LevelOptions.Clear();
            foreach (var option in FilterOption.ForItems("All levels", levels, item => item.Id, item => item.DisplayName))
            {
                LevelOptions.Add(option);
            }

            ClassOptions.Clear();
            foreach (var option in FilterOption.ForItems("All groups", classes, item => item.Id, item => item.DisplayName))
            {
                ClassOptions.Add(option);
            }

            RebuildBillMenu(paymentTypes);

            _selectedLevel = LevelOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedLevel));

            _selectedClass = ClassOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedClass));
        });
    }

    private void RebuildBillMenu(IReadOnlyList<PaymentTypeOption> paymentTypes)
    {
        BillMenuItems.Clear();

        void AddKnown(string name, string label, PaymentFrequency frequency)
        {
            if (paymentTypes.Any(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                BillMenuItems.Add(new StudentBillMenuItem(name, label, frequency, IsWellKnown: true));
            }
        }

        AddKnown(WellKnownPaymentTypes.Ecolage, "Écolage (monthly)", PaymentFrequency.Monthly);
        AddKnown(WellKnownPaymentTypes.Droit, "Droit (one per school year)", PaymentFrequency.OneTime);
        AddKnown(WellKnownPaymentTypes.Livre, "Livre", PaymentFrequency.OneTime);
        AddKnown(WellKnownPaymentTypes.MockExam, "Mock Exam", PaymentFrequency.OneTime);
        AddKnown(WellKnownPaymentTypes.OfficialExam, "Official Exam", PaymentFrequency.OneTime);

        foreach (var type in paymentTypes
                     .Where(type => !WellKnownPaymentTypes.IsWellKnown(type.Name))
                     .OrderBy(type => type.Name))
        {
            var label = type.Frequency == PaymentFrequency.Monthly
                ? $"{type.Name} (monthly)"
                : type.Name;
            BillMenuItems.Add(new StudentBillMenuItem(type.Name, label, type.Frequency, IsWellKnown: false));
        }
    }

    private async Task CreateAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<StudentEditViewModel>();
        viewModel.InitializeForCreate();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await ReloadFirstPageAsync();
            StatusMessage = $"Student {viewModel.StudentNumberLabel} has been created.";
        }
    }

    private async Task EditAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<StudentEditViewModel>();
        viewModel.InitializeForEdit(SelectedItem.Id);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await ReloadCurrentPageAsync();
            StatusMessage = "The student file has been updated.";
        }
    }

    private async Task TransferAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<StudentTransferViewModel>();
        viewModel.Initialize(SelectedItem.Id, SelectedItem.FullName, SelectedItem.StudentGroupName);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            var name = SelectedItem.FullName;
            await ReloadCurrentPageAsync();
            StatusMessage = $"{name} has been transferred.";
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var student = SelectedItem;

        if (!_dialogService.ConfirmCritical(
                $"Archive the file of {student.FullName} ({student.StudentNumber})?\n\n"
                + "The student leaves the active lists but the invoices, payments and receipts are kept.",
                "Archive a student"))
        {
            return;
        }

        var deleted = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IStudentService>().DeleteAsync(student.Id)));

        if (deleted)
        {
            await ReloadCurrentPageAsync();
            StatusMessage = $"The file of {student.FullName} has been archived.";
        }
    }

    private Task OpenDetailAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        var studentId = SelectedItem.Id;

        return _navigationService.NavigateToAsync<StudentDetailViewModel>(
            viewModel => viewModel.Initialize(studentId));
    }

    private async Task CombinedBillAndPayAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<CombinedBillAndPayViewModel>();
        viewModel.Initialize(SelectedItem.Id, SelectedItem.FullName);

        if (!await _dialogService.ShowDialogAsync(viewModel))
        {
            return;
        }

        await ReloadCurrentPageAsync();
        StatusMessage = viewModel.ResultSummary;
    }

    private async Task BillAsync(object? parameter)
    {
        if (SelectedItem is null || parameter is not StudentBillMenuItem item)
        {
            return;
        }

        var studentId = SelectedItem.Id;
        var studentName = SelectedItem.FullName;
        string? summary;

        if (item.Frequency == PaymentFrequency.Monthly)
        {
            var viewModel = _serviceProvider.GetRequiredService<MonthlyFeeGenerationViewModel>();
            viewModel.ConfigureForStudent(studentId, item.Key);
            if (!await _dialogService.ShowDialogAsync(viewModel))
            {
                return;
            }

            summary = viewModel.ResultSummary;

            if (viewModel.CreatedFeeIds.Count > 0
                && _currentUser.HasPermission(Permission.RegisterPayments))
            {
                var paymentVm = _serviceProvider.GetRequiredService<PaymentDialogViewModel>();
                paymentVm.InitializeForFees(studentId, viewModel.CreatedFeeIds);
                if (await _dialogService.ShowDialogAsync(paymentVm)
                    && !string.IsNullOrWhiteSpace(paymentVm.ResultSummary))
                {
                    summary = $"{summary} {paymentVm.ResultSummary}";
                }
            }
        }
        else
        {
            var scope = PaymentTypeScope.Named(item.Key);
            var title = item.IsWellKnown && WellKnownPaymentTypes.IsOnePerSchoolYear(item.Key)
                ? $"Bill and pay {item.Key} — {studentName}"
                : $"Create bill and pay — {item.Key} — {studentName}";

            summary = await BillOneTimeAsync(scope, title, studentId);
            if (summary is null)
            {
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(summary))
        {
            await ReloadCurrentPageAsync();
            StatusMessage = summary;
        }
    }

    private async Task<string?> BillOneTimeAsync(PaymentTypeScope scope, string title, int studentId)
    {
        var viewModel = _serviceProvider.GetRequiredService<OneTimeFeeGenerationViewModel>();
        viewModel.ConfigureForStudent(scope, title, studentId);

        if (!await _dialogService.ShowDialogAsync(viewModel))
        {
            return null;
        }

        return viewModel.ResultSummary;
    }

    protected override void OnSelectionChanged() => RaiseSelectionCommands();

    private void RaiseSelectionCommands()
    {
        (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenDetailCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (TransferCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (BillCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CombinedBillAndPayCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
