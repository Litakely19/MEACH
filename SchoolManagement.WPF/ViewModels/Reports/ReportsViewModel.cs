using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Converters;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Reports;

public enum ReportSection
{
    Daily = 0,
    Monthly = 1,
    ByClass = 2,
    ByPaymentType = 3,
    Outstanding = 4
}

/// <summary>
/// Financial reporting: cash of the day, monthly income, collection per class and
/// per payment type, and the students still owing money. Every section shares the
/// same filters and the same PDF and Excel exports.
/// </summary>
public class ReportsViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDocumentService _documentService;

    private ReportSection _section = ReportSection.Daily;
    private FilterOption<int>? _selectedAcademicLevel;
    private FilterOption<int>? _selectedClass;
    private FilterOption<int>? _selectedPaymentType;
    private DateTime? _date = DateTime.Today;
    private DateTime? _from = new DateTime(DateTime.Today.Year, 1, 1);
    private DateTime? _to = DateTime.Today;
    private DailyPaymentReport _daily = DailyPaymentReport.Empty(DateTime.Today);
    private MonthlyFinancialReport _monthly = MonthlyFinancialReport.Empty;
    private bool _isInitialized;

    public ReportsViewModel(
        ILogger<ReportsViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDocumentService documentService) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _documentService = documentService;

        RunCommand = new AsyncRelayCommand(LoadSectionAsync);
        ExportPdfCommand = new AsyncRelayCommand(() => ExportAsync(ExportFormat.Pdf));
        ExportExcelCommand = new AsyncRelayCommand(() => ExportAsync(ExportFormat.Excel));
    }

    public ObservableCollection<FilterOption<int>> AcademicLevelOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> ClassOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> PaymentTypeOptions { get; } = new();

    public ObservableCollection<PaymentMethodBreakdownRow> DailyMethods { get; } = new();

    public ObservableCollection<MonthlyFinancialRow> MonthlyRows { get; } = new();

    public ObservableCollection<GroupFinancialRow> ClassRows { get; } = new();

    public ObservableCollection<PaymentTypeFinancialRow> PaymentTypeRows { get; } = new();

    public ObservableCollection<OutstandingBalanceRow> OutstandingRows { get; } = new();

    public ICommand RunCommand { get; }

    public ICommand ExportPdfCommand { get; }

    public ICommand ExportExcelCommand { get; }

    /// <summary>Bound to the selected tab; changing it loads that section only.</summary>
    public int SelectedSectionIndex
    {
        get => (int)_section;
        set
        {
            var section = (ReportSection)value;

            if (_section == section)
            {
                return;
            }

            _section = section;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowsSingleDate));
            OnPropertyChanged(nameof(ShowsDateRange));

            if (_isInitialized)
            {
                _ = LoadSectionAsync();
            }
        }
    }

    public bool ShowsSingleDate => _section == ReportSection.Daily;

    public bool ShowsDateRange => _section != ReportSection.Daily;

    public DailyPaymentReport Daily
    {
        get => _daily;
        private set => SetProperty(ref _daily, value);
    }

    public MonthlyFinancialReport Monthly
    {
        get => _monthly;
        private set => SetProperty(ref _monthly, value);
    }

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

    public DateTime? Date
    {
        get => _date;
        set => SetFilter(ref _date, value);
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
        await LoadSectionAsync();
        _isInitialized = true;
    }

    private ReportFilter BuildFilter() => new(
        From,
        To,
        SelectedAcademicLevel?.Value,
        SelectedClass?.Value,
        SelectedPaymentType?.Value);

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
                var typeOptions = await provider.GetRequiredService<IPaymentTypeService>().ListOptionsAsync();

                return (levelOptions, classOptions, typeOptions);
            });

            Replace(AcademicLevelOptions, FilterOption.ForItems("All levels", levels, level => level.Id, level => level.DisplayName));
            Replace(ClassOptions, FilterOption.ForItems("All groups", classes, item => item.Id, item => item.DisplayName));
            Replace(PaymentTypeOptions, FilterOption.ForItems("All payment types", paymentTypes, type => type.Id, type => type.Name));

            _selectedAcademicLevel = AcademicLevelOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedAcademicLevel));

            _selectedClass = ClassOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedClass));

            _selectedPaymentType = PaymentTypeOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedPaymentType));
        });
    }

    private Task LoadSectionAsync() =>
        RunGuardedAsync(async () =>
        {
            await _scopedExecutor.RunAsync(async provider =>
            {
                var service = provider.GetRequiredService<IReportService>();
                var filter = BuildFilter();

                switch (_section)
                {
                    case ReportSection.Daily:
                        Daily = await service.GetDailyPaymentsAsync(Date ?? DateTime.Today);
                        Replace(DailyMethods, Daily.ByMethod);
                        break;

                    case ReportSection.Monthly:
                        Monthly = await service.GetMonthlyFinancialReportAsync(filter);
                        Replace(MonthlyRows, Monthly.Rows);
                        break;

                    case ReportSection.ByClass:
                        Replace(ClassRows, await service.GetPaymentsByGroupAsync(filter));
                        break;

                    case ReportSection.ByPaymentType:
                        Replace(PaymentTypeRows, await service.GetPaymentsByTypeAsync(filter));
                        break;

                    case ReportSection.Outstanding:
                        Replace(OutstandingRows, await service.GetOutstandingBalancesAsync(filter));
                        break;
                }
            });
        });

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void SetFilter<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);

        if (_isInitialized)
        {
            _ = LoadSectionAsync();
        }
    }

    private Task ExportAsync(ExportFormat format) =>
        RunGuardedAsync(async () =>
        {
            var report = _section switch
            {
                ReportSection.Daily => await BuildDailyReportAsync(),
                ReportSection.Monthly => await BuildMonthlyReportAsync(),
                ReportSection.ByClass => await BuildClassReportAsync(),
                ReportSection.ByPaymentType => await BuildPaymentTypeReportAsync(),
                _ => await BuildOutstandingReportAsync()
            };

            if (report is null)
            {
                ErrorMessage = "This report has no data to export.";
                return;
            }

            var path = await _documentService.ExportReportAsync(report, format);

            if (path is not null)
            {
                StatusMessage = $"Report written to {path}";
            }
        });

    private async Task<TabularReport?> BuildDailyReportAsync()
    {
        if (DailyMethods.Count == 0)
        {
            return null;
        }

        return await _documentService.BuildReportAsync(
            "Daily payments",
            $"{Daily.Date:D} - {Daily.PaymentCount} payments for {Money.Format(Daily.TotalCollected)}",
            new[]
            {
                new ReportColumn("Payment method", ReportColumnAlignment.Left, 2f),
                new ReportColumn("Payments", ReportColumnAlignment.Right, 1f),
                new ReportColumn("Amount", ReportColumnAlignment.Right, 1.4f)
            },
            DailyMethods.Select(row => (IReadOnlyList<string>)new[]
            {
                EnumDisplayConverter.Humanize(row.PaymentMethod.ToString()),
                row.PaymentCount.ToString(),
                Money.FormatAmount(row.Amount)
            }).ToList(),
            new[]
            {
                "Total",
                Daily.PaymentCount.ToString(),
                Money.FormatAmount(Daily.TotalCollected)
            });
    }

    private async Task<TabularReport?> BuildMonthlyReportAsync()
    {
        if (MonthlyRows.Count == 0)
        {
            return null;
        }

        return await _documentService.BuildReportAsync(
            "Monthly financial report",
            BuildRangeSubtitle(),
            new[]
            {
                new ReportColumn("Period", ReportColumnAlignment.Left, 1.4f),
                new ReportColumn("Expected", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Collected", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Remaining", ReportColumnAlignment.Right, 1.2f)
            },
            MonthlyRows.Select(row => (IReadOnlyList<string>)new[]
            {
                row.PeriodLabel,
                Money.FormatAmount(row.ExpectedAmount),
                Money.FormatAmount(row.CollectedAmount),
                Money.FormatAmount(row.RemainingAmount)
            }).ToList(),
            new[]
            {
                "Total",
                Money.FormatAmount(Monthly.TotalExpected),
                Money.FormatAmount(Monthly.TotalCollected),
                Money.FormatAmount(Monthly.TotalRemaining)
            });
    }

    private async Task<TabularReport?> BuildClassReportAsync()
    {
        if (ClassRows.Count == 0)
        {
            return null;
        }

        return await _documentService.BuildReportAsync(
            "Payments by class",
            BuildRangeSubtitle(),
            new[]
            {
                new ReportColumn("Class", ReportColumnAlignment.Left, 1.4f),
                new ReportColumn("Level", ReportColumnAlignment.Left, 1.2f),
                new ReportColumn("Students", ReportColumnAlignment.Right, 0.9f),
                new ReportColumn("Expected", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Collected", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Remaining", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Rate", ReportColumnAlignment.Right, 0.8f)
            },
            ClassRows.Select(row => (IReadOnlyList<string>)new[]
            {
                row.StudentGroupName,
                row.AcademicLevelName,
                row.StudentCount.ToString(),
                Money.FormatAmount(row.ExpectedAmount),
                Money.FormatAmount(row.CollectedAmount),
                Money.FormatAmount(row.RemainingAmount),
                $"{row.CollectionRate}%"
            }).ToList(),
            new[]
            {
                "Total",
                string.Empty,
                ClassRows.Sum(row => row.StudentCount).ToString(),
                Money.FormatAmount(ClassRows.Sum(row => row.ExpectedAmount)),
                Money.FormatAmount(ClassRows.Sum(row => row.CollectedAmount)),
                Money.FormatAmount(ClassRows.Sum(row => row.RemainingAmount)),
                string.Empty
            });
    }

    private async Task<TabularReport?> BuildPaymentTypeReportAsync()
    {
        if (PaymentTypeRows.Count == 0)
        {
            return null;
        }

        return await _documentService.BuildReportAsync(
            "Payments by type",
            BuildRangeSubtitle(),
            new[]
            {
                new ReportColumn("Payment type", ReportColumnAlignment.Left, 2f),
                new ReportColumn("Expected", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Collected", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Remaining", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Rate", ReportColumnAlignment.Right, 0.8f)
            },
            PaymentTypeRows.Select(row => (IReadOnlyList<string>)new[]
            {
                row.PaymentTypeName,
                Money.FormatAmount(row.ExpectedAmount),
                Money.FormatAmount(row.CollectedAmount),
                Money.FormatAmount(row.RemainingAmount),
                $"{row.CollectionRate}%"
            }).ToList(),
            new[]
            {
                "Total",
                Money.FormatAmount(PaymentTypeRows.Sum(row => row.ExpectedAmount)),
                Money.FormatAmount(PaymentTypeRows.Sum(row => row.CollectedAmount)),
                Money.FormatAmount(PaymentTypeRows.Sum(row => row.RemainingAmount)),
                string.Empty
            });
    }

    private async Task<TabularReport?> BuildOutstandingReportAsync()
    {
        if (OutstandingRows.Count == 0)
        {
            return null;
        }

        return await _documentService.BuildReportAsync(
            "Outstanding balances",
            BuildRangeSubtitle(),
            new[]
            {
                new ReportColumn("Student number", ReportColumnAlignment.Left, 1.1f),
                new ReportColumn("Student", ReportColumnAlignment.Left, 1.8f),
                new ReportColumn("Class", ReportColumnAlignment.Left, 1f),
                new ReportColumn("Expected", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Paid", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Remaining", ReportColumnAlignment.Right, 1.2f),
                new ReportColumn("Overdue lines", ReportColumnAlignment.Right, 1f),
                new ReportColumn("Oldest due date", ReportColumnAlignment.Center, 1.1f)
            },
            OutstandingRows.Select(row => (IReadOnlyList<string>)new[]
            {
                row.StudentNumber,
                row.StudentName,
                row.StudentGroupName,
                Money.FormatAmount(row.ExpectedAmount),
                Money.FormatAmount(row.PaidAmount),
                Money.FormatAmount(row.RemainingAmount),
                row.OverdueItemCount.ToString(),
                row.OldestDueDate?.ToString("d") ?? "-"
            }).ToList(),
            new[]
            {
                "Total",
                string.Empty,
                string.Empty,
                Money.FormatAmount(OutstandingRows.Sum(row => row.ExpectedAmount)),
                Money.FormatAmount(OutstandingRows.Sum(row => row.PaidAmount)),
                Money.FormatAmount(OutstandingRows.Sum(row => row.RemainingAmount)),
                OutstandingRows.Sum(row => row.OverdueItemCount).ToString(),
                string.Empty
            },
            landscape: true);
    }

    private string BuildRangeSubtitle()
    {
        var parts = new List<string> { $"From {From:d} to {To:d}" };

        if (SelectedAcademicLevel?.Value is not null)
        {
            parts.Add(SelectedAcademicLevel.Label);
        }

        if (SelectedClass?.Value is not null)
        {
            parts.Add($"Group {SelectedClass.Label}");
        }

        if (SelectedPaymentType?.Value is not null)
        {
            parts.Add(SelectedPaymentType.Label);
        }

        return string.Join(" - ", parts);
    }
}
