using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.Billing;

/// <summary>
/// Month by month tracking of the scholar fees, with the bulk billing action that
/// creates the obligations for a whole class or school year.
/// </summary>
public class MonthlyFeeViewModel : FeeListViewModelBase
{
    public MonthlyFeeViewModel(
        ILogger<MonthlyFeeViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
        : base(logger, scopedExecutor, dialogService, documentService, navigationService, currentUser, serviceProvider)
    {
        GenerateCommand = new AsyncRelayCommand(
            GenerateAsync,
            () => currentUser.HasPermission(Permission.ManageFees));
    }

    public ICommand GenerateCommand { get; }

    protected override PaymentTypeScope TypeScope => PaymentTypeScope.Ecolage;

    protected override string ReportTitle => "Monthly écolage";

    private async Task GenerateAsync()
    {
        var viewModel = ServiceProvider.GetRequiredService<MonthlyFeeGenerationViewModel>();

        if (!await DialogService.ShowDialogAsync(viewModel))
        {
            return;
        }

        await ReloadAsync();
        StatusMessage = viewModel.ResultSummary;
    }
}
