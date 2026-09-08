using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Payments;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Converters;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.Payments;

/// <summary>
/// Full payment history with the filters a cashier and an accountant need at the
/// end of the day, and the exports for the daily cash report.
/// </summary>
public class PaymentHistoryViewModel : PagedListViewModel<PaymentListItem>
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private FilterOption<int>? _selectedAcademicLevel;
    private FilterOption<int>? _selectedClass;
    private FilterOption<int>? _selectedPaymentType;
    private FilterOption<int>? _selectedCashier;
    private FilterOption<PaymentMethod>? _selectedMethod;
    private FilterOption<PaymentStatus>? _selectedStatus;
    private DateTime? _from = DateTime.Today.AddDays(-30);
    private DateTime? _to = DateTime.Today;
    private decimal _pageTotal;

    public PaymentHistoryViewModel(
        ILogger<PaymentHistoryViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _documentService = documentService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        MethodOptions = FilterOption.ForEnum<PaymentMethod>("All methods");
        _selectedMethod = MethodOptions[0];

        StatusOptions = FilterOption.ForEnum<PaymentStatus>("All statuses");
        _selectedStatus = StatusOptions[0];

        RegisterPaymentCommand = new AsyncRelayCommand(
            RegisterPaymentAsync,
            () => _currentUser.HasPermission(Permission.RegisterPayments));
        OpenDetailCommand = new AsyncRelayCommand(OpenDetailAsync, () => SelectedItem is not null);
        PrintReceiptCommand = new AsyncRelayCommand(
            PrintReceiptAsync,
            () => SelectedItem?.ReceiptNumber is not null);
        ExportPdfCommand = new AsyncRelayCommand(() => ExportAsync(ExportFormat.Pdf));
        ExportExcelCommand = new AsyncRelayCommand(() => ExportAsync(ExportFormat.Excel));
        TodayCommand = new AsyncRelayCommand(SetTodayAsync);
    }

    public bool CanFilterByCashier => _currentUser.HasPermission(Permission.ViewUsers);

    public ObservableCollection<FilterOption<int>> AcademicLevelOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> ClassOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> PaymentTypeOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> CashierOptions { get; } = new();

    public IReadOnlyList<FilterOption<PaymentMethod>> MethodOptions { get; }

    public IReadOnlyList<FilterOption<PaymentStatus>> StatusOptions { get; }

    public ICommand RegisterPaymentCommand { get; }

    public ICommand OpenDetailCommand { get; }

    public ICommand PrintReceiptCommand { get; }

    public ICommand ExportPdfCommand { get; }

    public ICommand ExportExcelCommand { get; }

    public ICommand TodayCommand { get; }

    /// <summary>Total of the rows currently displayed, the figure a cashier reconciles.</summary>
    public decimal PageTotal
    {
        get => _pageTotal;
        private set
        {
            if (SetProperty(ref _pageTotal, value))
            {
                OnPropertyChanged(nameof(PageTotalLabel));
            }
        }
    }

    public string PageTotalLabel => Money.Format(PageTotal);

    public FilterOption<int>? SelectedAcademicLevel
    {
        get => _selectedAcademicLevel;
        set => SetFilter(ref _selectedAcademicLevel, value);
    }

    public FilterOption<int>? SelectedClass
    {
        get => _selectedClass;
        set => SetFilter(ref _selectedClass, value);
    }

    public FilterOption<int>? SelectedPaymentType
    {
        get => _selectedPaymentType;
        set => SetFilter(ref _selectedPaymentType, value);
    }

    public FilterOption<int>? SelectedCashier
    {
        get => _selectedCashier;
        set => SetFilter(ref _selectedCashier, value);
    }

    public FilterOption<PaymentMethod>? SelectedMethod
    {
        get => _selectedMethod;
        set => SetFilter(ref _selectedMethod, value);
    }

    public FilterOption<PaymentStatus>? SelectedStatus
    {
        get => _selectedStatus;
        set => SetFilter(ref _selectedStatus, value);
    }

    public DateTime? From
    {
        get => _from;
        set => SetFilter(ref _from, value);
    }

    public DateTime? To
    {
        get => _to;
        set => SetFilter(ref _to, value);
    }

    public override async Task LoadAsync()
    {
        await LoadLookupsAsync();
        await base.LoadAsync();
    }

    protected override Task<PagedResult<PaymentListItem>> FetchAsync(int page) =>
        _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().ListAsync(BuildFilter(page)));

    protected override Task OnPageLoadedAsync()
    {
        PageTotal = Items
            .Where(item => item.Status == PaymentStatus.Active)
            .Sum(item => item.Amount);

        RaiseSelectionCommands();
        return Task.CompletedTask;
    }

    protected override void OnSelectionChanged() => RaiseSelectionCommands();

    private PaymentFilter BuildFilter(int page) => new(
        SearchTerm,
        StudentId: null,
        AcademicLevelId: SelectedAcademicLevel?.Value,
        StudentGroupId: SelectedClass?.Value,
        SelectedPaymentType?.Value,
        SelectedMethod?.Value,
        SelectedStatus?.Value,
        SelectedCashier?.Value,
        From,
        To,
        page,
        PageSize);

    private async Task LoadLookupsAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var (levels, classes, paymentTypes, cashiers) = await _scopedExecutor.RunAsync(async provider =>
            {
                var levelOptions = await provider.GetRequiredService<IAcademicLevelService>()
                    .ListOptionsAsync(onlyActive: false);
                var classOptions = await provider.GetRequiredService<IStudentGroupService>()
                    .ListOptionsAsync(onlyActive: false);
                var typeOptions = await provider.GetRequiredService<IPaymentTypeService>().ListOptionsAsync();

                // Only roles that may see the user directory get the cashier filter.
                var userOptions = CanFilterByCashier
                    ? await provider.GetRequiredService<IUserService>().ListAsync()
                    : Array.Empty<Application.DTOs.Users.UserListItem>();

                return (levelOptions, classOptions, typeOptions, userOptions);
            });

            Replace(AcademicLevelOptions, FilterOption.ForItems("All levels", levels, level => level.Id, level => level.DisplayName));
            Replace(ClassOptions, FilterOption.ForItems("All groups", classes, item => item.Id, item => item.DisplayName));
            Replace(PaymentTypeOptions, FilterOption.ForItems("All payment types", paymentTypes, type => type.Id, type => type.Name));
            Replace(CashierOptions, FilterOption.ForItems("All users", cashiers, user => user.Id, user => user.FullName));

            SetFilterSilently(ref _selectedAcademicLevel, AcademicLevelOptions.FirstOrDefault(), nameof(SelectedAcademicLevel));
            SetFilterSilently(ref _selectedClass, ClassOptions.FirstOrDefault(), nameof(SelectedClass));
            SetFilterSilently(ref _selectedPaymentType, PaymentTypeOptions.FirstOrDefault(), nameof(SelectedPaymentType));
            SetFilterSilently(ref _selectedCashier, CashierOptions.FirstOrDefault(), nameof(SelectedCashier));
        });
    }

    private static void Replace(ObservableCollection<FilterOption<int>> target, IEnumerable<FilterOption<int>> source)
    {
        target.Clear();
        foreach (var option in source)
        {
            target.Add(option);
        }
    }

    private async Task SetTodayAsync()
    {
        SetFilterSilently(ref _from, DateTime.Today, nameof(From));
        SetFilterSilently(ref _to, DateTime.Today, nameof(To));

        await ReloadFirstPageAsync();
    }

    private async Task RegisterPaymentAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<PaymentDialogViewModel>();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await ReloadFirstPageAsync();
            StatusMessage = viewModel.ResultSummary;
        }
    }

    private async Task OpenDetailAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<PaymentDetailViewModel>();
        viewModel.Initialize(SelectedItem.Id);

        await _dialogService.ShowDialogAsync(viewModel);
        await ReloadCurrentPageAsync();
    }

    private Task PrintReceiptAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        var paymentId = SelectedItem.Id;

        return RunGuardedAsync(async () =>
        {
            var path = await _documentService.ExportReceiptByPaymentAsync(paymentId);

            if (path is not null)
            {
                StatusMessage = $"Receipt written to {path}";
            }
        });
    }

    private Task ExportAsync(ExportFormat format) =>
        RunGuardedAsync(async () =>
        {
            // The export covers the whole filtered range, not only the visible page.
            var result = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IPaymentService>().ListAsync(BuildFilter(1) with { PageSize = 5000 }));

            var rows = result.Items;

            if (rows.Count == 0)
            {
                ErrorMessage = "There is nothing to export with these filters.";
                return;
            }

            var report = await _documentService.BuildReportAsync(
                "Payment history",
                BuildSubtitle(),
                new[]
                {
                    new ReportColumn("Payment", ReportColumnAlignment.Left, 1.2f),
                    new ReportColumn("Date", ReportColumnAlignment.Center, 1f),
                    new ReportColumn("Student", ReportColumnAlignment.Left, 1.8f),
                    new ReportColumn("Group", ReportColumnAlignment.Left, 1f),
                    new ReportColumn("Type", ReportColumnAlignment.Left, 1.2f),
                    new ReportColumn("Amount", ReportColumnAlignment.Right, 1.2f),
                    new ReportColumn("Method", ReportColumnAlignment.Left, 1.1f),
                    new ReportColumn("Received by", ReportColumnAlignment.Left, 1.3f),
                    new ReportColumn("Status", ReportColumnAlignment.Left, 1f)
                },
                rows.Select(row => (IReadOnlyList<string>)new[]
                {
                    row.PaymentNumber,
                    row.PaymentDate.ToString("d"),
                    row.StudentName,
                    row.StudentGroupName,
                    row.PaymentTypeName,
                    Money.FormatAmount(row.Amount),
                    EnumDisplayConverter.Humanize(row.PaymentMethod.ToString()),
                    row.ReceivedBy,
                    EnumDisplayConverter.Humanize(row.Status.ToString())
                }).ToList(),
                new[]
                {
                    "Total",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    Money.FormatAmount(rows.Where(row => row.Status == PaymentStatus.Active).Sum(row => row.Amount)),
                    string.Empty,
                    string.Empty,
                    string.Empty
                },
                landscape: true);

            var path = await _documentService.ExportReportAsync(report, format);

            if (path is not null)
            {
                StatusMessage = $"Export written to {path}";
            }
        });

    private string BuildSubtitle()
    {
        var parts = new List<string>();

        if (From is not null || To is not null)
        {
            parts.Add($"From {From:d} to {To:d}");
        }

        if (SelectedClass?.Value is not null)
        {
            parts.Add($"Group {SelectedClass.Label}");
        }

        if (SelectedMethod?.Value is not null)
        {
            parts.Add(SelectedMethod.Label);
        }

        if (SelectedCashier?.Value is not null)
        {
            parts.Add($"Received by {SelectedCashier.Label}");
        }

        return parts.Count == 0 ? "All payments" : string.Join(" - ", parts);
    }

    private void RaiseSelectionCommands()
    {
        (OpenDetailCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintReceiptCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
