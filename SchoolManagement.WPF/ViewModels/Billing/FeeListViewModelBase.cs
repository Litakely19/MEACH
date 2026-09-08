using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Converters;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;
using SchoolManagement.WPF.ViewModels.People;

namespace SchoolManagement.WPF.ViewModels.Billing;

/// <summary>
/// Shared behaviour of the three fee tracking screens (monthly scholar fees, one
/// time fees and unpaid fees): the same filters, the same totals and the same
/// exports over <see cref="StudentFeeItem"/>, only the payment types differ.
/// </summary>
public abstract class FeeListViewModelBase : PagedListViewModel<StudentFeeItem>
{
    private readonly IDocumentService _documentService;
    private readonly INavigationService _navigationService;

    private FilterOption<int>? _selectedAcademicLevel;
    private FilterOption<int>? _selectedStudentGroup;
    private FilterOption<int>? _selectedPaymentType;
    private FilterOption<int>? _selectedMonth;
    private FilterOption<FeeStatus>? _selectedStatus;
    private bool _onlyOutstanding;
    private FeeSummary _summary = FeeSummary.Empty;

    protected FeeListViewModelBase(
        ILogger logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        ScopedExecutor = scopedExecutor;
        DialogService = dialogService;
        CurrentUser = currentUser;
        ServiceProvider = serviceProvider;
        _documentService = documentService;
        _navigationService = navigationService;

        _onlyOutstanding = DefaultToOutstandingOnly;

        StatusOptions = FilterOption.ForEnum<FeeStatus>("All statuses");
        _selectedStatus = StatusOptions[0];

        MonthOptions = BuildMonthOptions();
        _selectedMonth = MonthOptions[0];

        ExportPdfCommand = new AsyncRelayCommand(() => ExportAsync(ExportFormat.Pdf));
        ExportExcelCommand = new AsyncRelayCommand(() => ExportAsync(ExportFormat.Excel));
        RegisterPaymentCommand = new AsyncRelayCommand(
            RegisterPaymentAsync,
            () => SelectedItem is not null && CurrentUser.HasPermission(Permission.RegisterPayments));
        OpenStudentCommand = new AsyncRelayCommand(OpenStudentAsync, () => SelectedItem is not null);
    }

    protected IScopedExecutor ScopedExecutor { get; }

    protected IDialogService DialogService { get; }

    protected ICurrentUserService CurrentUser { get; }

    protected IServiceProvider ServiceProvider { get; }

    /// <summary>Restricts the payment type picker and the listed fees to this screen's scope.</summary>
    protected virtual PaymentTypeScope TypeScope => PaymentTypeScope.All;

    protected abstract string ReportTitle { get; }

    protected virtual bool DefaultToOutstandingOnly => false;

    public virtual bool ShowPaymentTypeColumn => true;

    public virtual bool ShowPeriodColumn => true;

    public virtual bool ShowDescriptionColumn => false;

    public ObservableCollection<FilterOption<int>> AcademicLevelOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> StudentGroupOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> PaymentTypeOptions { get; } = new();

    public IReadOnlyList<FilterOption<FeeStatus>> StatusOptions { get; }

    public IReadOnlyList<FilterOption<int>> MonthOptions { get; }

    public ICommand ExportPdfCommand { get; }

    public ICommand ExportExcelCommand { get; }

    public ICommand RegisterPaymentCommand { get; }

    public ICommand OpenStudentCommand { get; }

    public FeeSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public FilterOption<int>? SelectedAcademicLevel
    {
        get => _selectedAcademicLevel;
        set => SetFilter(ref _selectedAcademicLevel, value);
    }

    public FilterOption<int>? SelectedStudentGroup
    {
        get => _selectedStudentGroup;
        set => SetFilter(ref _selectedStudentGroup, value);
    }

    public FilterOption<int>? SelectedPaymentType
    {
        get => _selectedPaymentType;
        set => SetFilter(ref _selectedPaymentType, value);
    }

    public FilterOption<int>? SelectedMonth
    {
        get => _selectedMonth;
        set => SetFilter(ref _selectedMonth, value);
    }

    public FilterOption<FeeStatus>? SelectedStatus
    {
        get => _selectedStatus;
        set => SetFilter(ref _selectedStatus, value);
    }

    public bool OnlyOutstanding
    {
        get => _onlyOutstanding;
        set => SetFilter(ref _onlyOutstanding, value);
    }

    public override async Task LoadAsync()
    {
        await LoadLookupsAsync();
        await base.LoadAsync();
    }

    protected override Task<PagedResult<StudentFeeItem>> FetchAsync(int page) =>
        ScopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IFeeService>().ListAsync(BuildFilter(page)));

    protected override async Task OnPageLoadedAsync()
    {
        RaiseSelectionCommands();

        Summary = await ScopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IFeeService>().GetSummaryAsync(BuildFilter(1)));
    }

    protected override void OnSelectionChanged() => RaiseSelectionCommands();

    protected FeeFilter BuildFilter(int page) => new(
        SelectedAcademicLevel?.Value,
        SelectedStudentGroup?.Value,
        StudentId: null,
        SelectedPaymentType?.Value,
        SelectedMonth?.Value,
        Year: null,
        SelectedStatus?.Value,
        OnlyOutstanding,
        SearchTerm,
        page,
        PageSize,
        Scope: TypeScope);

    /// <summary>Reloads the fee lines and the totals after a billing or payment action.</summary>
    protected Task ReloadAsync() => ReloadCurrentPageAsync();

    private async Task LoadLookupsAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var (levels, groups, paymentTypes) = await ScopedExecutor.RunAsync(async provider =>
            {
                var levelOptions = await provider.GetRequiredService<IAcademicLevelService>().ListOptionsAsync(onlyActive: false);
                var groupOptions = await provider.GetRequiredService<IStudentGroupService>()
                    .ListOptionsAsync(onlyActive: false);
                var typeOptions = await provider.GetRequiredService<IPaymentTypeService>()
                    .ListOptionsAsync(TypeScope);

                return (levelOptions, groupOptions, typeOptions);
            });

            Replace(AcademicLevelOptions, FilterOption.ForItems("All levels", levels, item => item.Id, item => item.DisplayName));
            Replace(StudentGroupOptions, FilterOption.ForItems("All groups", groups, item => item.Id, item => item.DisplayName));
            Replace(PaymentTypeOptions, FilterOption.ForItems("All payment types", paymentTypes, type => type.Id, type => type.Name));

            _selectedAcademicLevel = AcademicLevelOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedAcademicLevel));

            _selectedStudentGroup = StudentGroupOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedStudentGroup));

            _selectedPaymentType = PaymentTypeOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedPaymentType));
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

    private static IReadOnlyList<FilterOption<int>> BuildMonthOptions()
    {
        var options = new List<FilterOption<int>> { new(null, "All months") };

        for (var month = 1; month <= 12; month++)
        {
            options.Add(new FilterOption<int>(month, Period.MonthName(month)));
        }

        return options;
    }

    private async Task ExportAsync(ExportFormat format)
    {
        await RunGuardedAsync(async () =>
        {
            var rows = await ScopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IFeeService>().ListAllAsync(BuildFilter(1)));

            if (rows.Count == 0)
            {
                ErrorMessage = "There is nothing to export with these filters.";
                return;
            }

            var columns = new List<ReportColumn>
            {
                new("Student number", ReportColumnAlignment.Left, 1.1f),
                new("Student", ReportColumnAlignment.Left, 2f),
                new("Group", ReportColumnAlignment.Left, 1.4f)
            };

            if (ShowPaymentTypeColumn)
            {
                columns.Add(new ReportColumn("Payment type", ReportColumnAlignment.Left, 1.4f));
            }

            if (ShowPeriodColumn)
            {
                columns.Add(new ReportColumn("Period", ReportColumnAlignment.Left, 1.1f));
            }

            if (ShowDescriptionColumn)
            {
                columns.Add(new ReportColumn("Description", ReportColumnAlignment.Left, 2f));
            }

            columns.AddRange(new[]
            {
                new ReportColumn("Expected", ReportColumnAlignment.Right, 1.1f),
                new ReportColumn("Paid", ReportColumnAlignment.Right, 1.1f),
                new ReportColumn("Remaining", ReportColumnAlignment.Right, 1.1f),
                new ReportColumn("Due date", ReportColumnAlignment.Center, 1f),
                new ReportColumn("Status", ReportColumnAlignment.Left, 1f)
            });

            var report = await _documentService.BuildReportAsync(
                ReportTitle,
                BuildSubtitle(),
                columns,
                rows.Select(row =>
                {
                    var cells = new List<string>
                    {
                        row.StudentNumber,
                        row.StudentName,
                        row.StudentGroupName
                    };

                    if (ShowPaymentTypeColumn)
                    {
                        cells.Add(row.PaymentTypeName);
                    }

                    if (ShowPeriodColumn)
                    {
                        cells.Add(row.PeriodLabel);
                    }

                    if (ShowDescriptionColumn)
                    {
                        cells.Add(row.Description ?? string.Empty);
                    }

                    cells.Add(Money.FormatAmount(row.ExpectedAmount));
                    cells.Add(Money.FormatAmount(row.PaidAmount));
                    cells.Add(Money.FormatAmount(row.RemainingAmount));
                    cells.Add(row.DueDate.ToString("d"));
                    cells.Add(EnumDisplayConverter.Humanize(row.Status.ToString()));
                    return (IReadOnlyList<string>)cells;
                }).ToList(),
                BuildExportTotals(rows, columns.Count),
                landscape: true);

            var path = await _documentService.ExportReportAsync(report, format);

            if (path is not null)
            {
                StatusMessage = $"Export written to {path}";
            }
        });
    }

    private string BuildSubtitle()
    {
        var parts = new List<string>();

        if (SelectedAcademicLevel?.Value is not null)
        {
            parts.Add(SelectedAcademicLevel.Label);
        }

        if (SelectedStudentGroup?.Value is not null)
        {
            parts.Add(SelectedStudentGroup.Label);
        }

        if (SelectedPaymentType?.Value is not null)
        {
            parts.Add(SelectedPaymentType.Label);
        }

        if (SelectedMonth?.Value is not null)
        {
            parts.Add(SelectedMonth.Label);
        }

        if (SelectedStatus?.Value is not null)
        {
            parts.Add(SelectedStatus.Label);
        }

        if (OnlyOutstanding)
        {
            parts.Add("Outstanding only");
        }

        return parts.Count == 0 ? "All fee lines" : string.Join(" · ", parts);
    }

    private IReadOnlyList<string> BuildExportTotals(IReadOnlyList<StudentFeeItem> rows, int columnCount)
    {
        var totals = Enumerable.Repeat(string.Empty, columnCount).ToArray();
        totals[0] = "Total";

        // Expected / Paid / Remaining sit just before Due date and Status at the end.
        var remainingIndex = columnCount - 4;
        var paidIndex = columnCount - 5;
        var expectedIndex = columnCount - 6;
        totals[expectedIndex] = Money.FormatAmount(rows.Sum(row => row.ExpectedAmount));
        totals[paidIndex] = Money.FormatAmount(rows.Sum(row => row.PaidAmount));
        totals[remainingIndex] = Money.FormatAmount(rows.Sum(row => row.RemainingAmount));
        return totals;
    }

    private async Task RegisterPaymentAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var viewModel = ServiceProvider.GetRequiredService<PaymentDialogViewModel>();
        viewModel.Initialize(SelectedItem.StudentId, SelectedItem.StudentFeeId);

        if (await DialogService.ShowDialogAsync(viewModel))
        {
            await ReloadAsync();
            StatusMessage = "The payment has been registered.";
        }
    }

    private Task OpenStudentAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        var studentId = SelectedItem.StudentId;

        return _navigationService.NavigateToAsync<StudentDetailViewModel>(
            viewModel => viewModel.Initialize(studentId));
    }

    private void RaiseSelectionCommands()
    {
        (RegisterPaymentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenStudentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
