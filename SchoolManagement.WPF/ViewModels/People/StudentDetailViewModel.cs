using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.Students;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.People;

public class StudentDetailViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private int _studentId;
    private StudentDetail? _student;
    private StudentFeeSummary? _selectedFee;
    private StudentPaymentSummary? _selectedPayment;

    public StudentDetailViewModel(
        ILogger<StudentDetailViewModel> logger,
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

        BackCommand = new AsyncRelayCommand(() => _navigationService.NavigateToAsync<StudentListViewModel>());
        EditCommand = new AsyncRelayCommand(EditAsync, () => _currentUser.HasPermission(Permission.ManageStudents));
        RegisterPaymentCommand = new AsyncRelayCommand(
            RegisterPaymentAsync,
            () => _currentUser.HasPermission(Permission.RegisterPayments));
        OpenPaymentCommand = new AsyncRelayCommand(OpenPaymentAsync, () => SelectedPayment is not null);
    }

    public ObservableCollection<MonthlyFeeCell> MonthlyFees { get; } = new();

    public ObservableCollection<StudentFeeSummary> Fees { get; } = new();

    public ObservableCollection<StudentPaymentSummary> Payments { get; } = new();

    public ObservableCollection<StudentReceiptSummary> Receipts { get; } = new();

    public ICommand BackCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand RegisterPaymentCommand { get; }

    public ICommand OpenPaymentCommand { get; }

    public StudentDetail? Student
    {
        get => _student;
        private set => SetProperty(ref _student, value);
    }

    public StudentFeeSummary? SelectedFee
    {
        get => _selectedFee;
        set => SetProperty(ref _selectedFee, value);
    }

    public StudentPaymentSummary? SelectedPayment
    {
        get => _selectedPayment;
        set
        {
            if (SetProperty(ref _selectedPayment, value))
            {
                (OpenPaymentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public void Initialize(int studentId) => _studentId = studentId;

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var (detail, timeline) = await _scopedExecutor.RunAsync(async provider =>
            {
                var service = provider.GetRequiredService<IStudentService>();
                var student = await service.GetDetailAsync(_studentId);
                var year = student.SchoolYearId.HasValue
                    ? DateTime.Today.Year
                    : DateTime.Today.Year;
                var months = await service.GetMonthlyFeeTimelineAsync(_studentId, year);
                return (student, months);
            });

            Student = detail;
            Fill(MonthlyFees, timeline);
            Fill(Fees, detail.Fees);
            Fill(Payments, detail.Payments);
            Fill(Receipts, detail.Receipts);
        });

    private static void Fill<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private async Task EditAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<StudentEditViewModel>();
        viewModel.InitializeForEdit(_studentId);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The student file has been updated.";
        }
    }

    private async Task RegisterPaymentAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<PaymentDialogViewModel>();
        viewModel.Initialize(_studentId, SelectedFee?.Id);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The payment has been registered.";
        }
    }

    private async Task OpenPaymentAsync()
    {
        if (SelectedPayment is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<PaymentDetailViewModel>();
        viewModel.Initialize(SelectedPayment.Id);

        await _dialogService.ShowDialogAsync(viewModel);
        await LoadAsync();
    }
}
