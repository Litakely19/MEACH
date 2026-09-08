using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.DTOs.SchoolYears;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Overview;

/// <summary>
/// Opening screen: the six figures a head of school asks for first, plus the
/// payments registered most recently.
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private DashboardSummary _summary = DashboardSummary.Empty;
    private SchoolYearOption? _selectedSchoolYear;
    private bool _isInitialized;

    public DashboardViewModel(ILogger<DashboardViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
    }

    public ObservableCollection<SchoolYearOption> SchoolYears { get; } = new();

    public ObservableCollection<RecentPaymentRow> RecentPayments { get; } = new();

    public SchoolYearOption? SelectedSchoolYear
    {
        get => _selectedSchoolYear;
        set
        {
            if (SetProperty(ref _selectedSchoolYear, value) && _isInitialized)
            {
                _ = LoadSummaryAsync();
            }
        }
    }

    public DashboardSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public override async Task LoadAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var years = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<ISchoolYearService>().ListOptionsAsync());

            SchoolYears.Clear();
            foreach (var year in years)
            {
                SchoolYears.Add(year);
            }

            _selectedSchoolYear = years.FirstOrDefault(year => year.IsCurrent) ?? years.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedSchoolYear));

            await LoadSummaryCoreAsync();
            _isInitialized = true;
        });
    }

    private Task LoadSummaryAsync() => RunGuardedAsync(LoadSummaryCoreAsync);

    private async Task LoadSummaryCoreAsync()
    {
        var summary = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IReportService>().GetDashboardSummaryAsync());

        Summary = summary;

        RecentPayments.Clear();
        foreach (var payment in summary.RecentPayments)
        {
            RecentPayments.Add(payment);
        }
    }
}
