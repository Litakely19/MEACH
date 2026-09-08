using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Administration;
using SchoolManagement.WPF.ViewModels.Billing;
using SchoolManagement.WPF.ViewModels.Dialogs;
using SchoolManagement.WPF.ViewModels.Overview;
using SchoolManagement.WPF.ViewModels.Payments;
using SchoolManagement.WPF.ViewModels.People;
using SchoolManagement.WPF.ViewModels.Reports;

namespace SchoolManagement.WPF.ViewModels.Shell;

/// <summary>
/// The application shell: identity banner, permission filtered navigation and the
/// host of the active screen.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly INavigationService _navigationService;
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;

    private NavigationItem? _selectedItem;
    private string _schoolName = "School Management";
    private string _schoolYearLabel = string.Empty;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        ICurrentUserService currentUser,
        INavigationService navigationService,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IServiceProvider serviceProvider) : base(logger)
    {
        _currentUser = currentUser;
        _navigationService = navigationService;
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;

        _navigationService.CurrentChanged += (_, _) => OnPropertyChanged(nameof(CurrentViewModel));

        NavigationItems = new ObservableCollection<NavigationItem>(BuildMenu());
        GroupedNavigation = CollectionViewSource.GetDefaultView(NavigationItems);
        GroupedNavigation.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NavigationItem.Group)));

        NavigateCommand = new AsyncRelayCommand(
            parameter => NavigateAsync(parameter as NavigationItem),
            parameter => parameter is NavigationItem);

        RegisterPaymentCommand = new AsyncRelayCommand(
            RegisterPaymentAsync,
            () => _currentUser.HasPermission(Permission.RegisterPayments));

        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshCurrentAsync);
        LogoutCommand = new RelayCommand(RequestLogout);
    }

    /// <summary>Raised when the operator asks to end the session; the host swaps back to the login window.</summary>
    public event EventHandler? LogoutRequested;

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public ICollectionView GroupedNavigation { get; }

    public ViewModelBase? CurrentViewModel => _navigationService.Current;

    public ICommand NavigateCommand { get; }

    public ICommand RegisterPaymentCommand { get; }

    public ICommand ChangePasswordCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand LogoutCommand { get; }

    public string SchoolName
    {
        get => _schoolName;
        private set => SetProperty(ref _schoolName, value);
    }

    public string SchoolYearLabel
    {
        get => _schoolYearLabel;
        private set => SetProperty(ref _schoolYearLabel, value);
    }

    public string UserFullName => _currentUser.User?.FullName ?? string.Empty;

    public string UserRoleLabel => _currentUser.User?.RoleDisplayName ?? string.Empty;

    public string UserInitials
    {
        get
        {
            var name = _currentUser.User?.FullName;

            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length == 1
                ? parts[0][..1].ToUpperInvariant()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }

    public NavigationItem? SelectedItem
    {
        get => _selectedItem;
        private set => SetProperty(ref _selectedItem, value);
    }

    public override async Task LoadAsync()
    {
        await RunGuardedAsync(async () =>
        {
            await _scopedExecutor.RunAsync(async provider =>
            {
                var settings = await provider.GetRequiredService<ISchoolSettingsService>().GetAsync();
                SchoolName = settings.SchoolName;

                var currentYear = await provider.GetRequiredService<ISchoolYearService>().GetCurrentAsync();
                SchoolYearLabel = currentYear is null
                    ? "No current school year"
                    : $"School year {currentYear.Name}";
            });
        });

        await NavigateAsync(NavigationItems.FirstOrDefault());
    }

    private IEnumerable<NavigationItem> BuildMenu()
    {
        var menu = new[]
        {
            new NavigationItem("Dashboard", "Overview", typeof(DashboardViewModel), Permission.ViewDashboard),

            new NavigationItem("Students", "People", typeof(StudentListViewModel), Permission.ViewStudents),
            new NavigationItem("Academic levels", "People", typeof(AcademicLevelListViewModel), Permission.ViewAcademicLevels),
            new NavigationItem("Student groups", "People", typeof(SchoolClassListViewModel), Permission.ViewStudentGroups),
            new NavigationItem("School years", "People", typeof(SchoolYearListViewModel), Permission.ViewSchoolYears),

            new NavigationItem("Daily attendance", "Attendance", typeof(AttendanceSheetViewModel), Permission.ViewAttendance),
            new NavigationItem("Attendance reports", "Attendance", typeof(AttendanceReportViewModel), Permission.ViewAttendance),

            new NavigationItem("Droit", "Payments", typeof(DroitFeeViewModel), Permission.ViewFees),
            new NavigationItem("Écolage", "Payments", typeof(MonthlyFeeViewModel), Permission.ViewFees),
            new NavigationItem("Livre", "Payments", typeof(LivreFeeViewModel), Permission.ViewFees),
            new NavigationItem("Mock Exam", "Payments", typeof(MockExamFeeViewModel), Permission.ViewFees),
            new NavigationItem("Official Exam", "Payments", typeof(OfficialExamFeeViewModel), Permission.ViewFees),
            new NavigationItem("Other fees", "Payments", typeof(CustomOneTimeFeeViewModel), Permission.ViewFees),
            new NavigationItem("Unpaid balances", "Payments", typeof(UnpaidFeeViewModel), Permission.ViewFees),
            new NavigationItem("Payment history", "Payments", typeof(PaymentHistoryViewModel), Permission.ViewPayments),
            new NavigationItem("Receipts", "Payments", typeof(ReceiptListViewModel), Permission.ViewReceipts),

            new NavigationItem("Financial reports", "Reports", typeof(ReportsViewModel), Permission.ViewReports),

            new NavigationItem("Users", "Administration", typeof(UserListViewModel), Permission.ViewUsers),
            new NavigationItem("Payment types", "Administration", typeof(PaymentTypeListViewModel), Permission.ManagePaymentTypes),
            new NavigationItem("Audit log", "Administration", typeof(AuditLogViewModel), Permission.ManageSettings),
            new NavigationItem("Settings", "Administration", typeof(SettingsViewModel), Permission.ManageSettings)
        };

        return menu.Where(item => _currentUser.HasPermission(item.RequiredPermission));
    }

    private async Task NavigateAsync(NavigationItem? item)
    {
        if (item is null || ReferenceEquals(item, SelectedItem))
        {
            return;
        }

        foreach (var candidate in NavigationItems)
        {
            candidate.IsSelected = ReferenceEquals(candidate, item);
        }

        SelectedItem = item;

        await RunGuardedAsync(() => _navigationService.NavigateToAsync(item.ViewModelType, item.Configure));
    }

    private Task RefreshCurrentAsync() =>
        RunGuardedAsync(async () =>
        {
            if (CurrentViewModel is not null)
            {
                await CurrentViewModel.LoadAsync();
            }
        });

    private async Task RegisterPaymentAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<PaymentDialogViewModel>();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await RefreshCurrentAsync();
        }
    }

    private async Task ChangePasswordAsync()
    {
        var user = _currentUser.User;

        if (user is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<ChangePasswordViewModel>();
        viewModel.Initialize(user.Id, user.FullName, isMandatory: false);

        await _dialogService.ShowDialogAsync(viewModel);
    }

    private void RequestLogout() => LogoutRequested?.Invoke(this, EventArgs.Empty);
}
