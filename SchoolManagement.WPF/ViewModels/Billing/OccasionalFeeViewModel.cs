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

public abstract class OccasionalFeeViewModel : FeeListViewModelBase
{
    protected OccasionalFeeViewModel(
        ILogger logger,
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

    public abstract string GenerateButtonText { get; }

    protected abstract string GenerationDialogTitle { get; }

    private async Task GenerateAsync()
    {
        var viewModel = ServiceProvider.GetRequiredService<OneTimeFeeGenerationViewModel>();
        viewModel.Configure(TypeScope, GenerationDialogTitle);

        if (!await DialogService.ShowDialogAsync(viewModel))
        {
            return;
        }

        await ReloadAsync();
        StatusMessage = viewModel.ResultSummary;
    }
}

public sealed class DroitFeeViewModel : OccasionalFeeViewModel
{
    public DroitFeeViewModel(
        ILogger<DroitFeeViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
        : base(logger, scopedExecutor, dialogService, documentService, navigationService, currentUser, serviceProvider)
    {
    }

    public override string GenerateButtonText => "Bill Droit";

    protected override string GenerationDialogTitle => "Bill Droit";

    protected override PaymentTypeScope TypeScope => PaymentTypeScope.Droit;

    protected override string ReportTitle => "Droit";
}

public sealed class LivreFeeViewModel : OccasionalFeeViewModel
{
    public LivreFeeViewModel(
        ILogger<LivreFeeViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
        : base(logger, scopedExecutor, dialogService, documentService, navigationService, currentUser, serviceProvider)
    {
    }

    public override string GenerateButtonText => "Bill Livre";

    protected override string GenerationDialogTitle => "Bill Livre";

    protected override PaymentTypeScope TypeScope => PaymentTypeScope.Livre;

    protected override string ReportTitle => "Livre";

    public override bool ShowPaymentTypeColumn => false;

    public override bool ShowPeriodColumn => false;

    public override bool ShowDescriptionColumn => true;
}

public sealed class MockExamFeeViewModel : OccasionalFeeViewModel
{
    public MockExamFeeViewModel(
        ILogger<MockExamFeeViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
        : base(logger, scopedExecutor, dialogService, documentService, navigationService, currentUser, serviceProvider)
    {
    }

    public override string GenerateButtonText => "Bill Mock Exam";

    protected override string GenerationDialogTitle => "Bill Mock Exam";

    protected override PaymentTypeScope TypeScope => PaymentTypeScope.MockExam;

    protected override string ReportTitle => "Mock Exam";

    public override bool ShowPaymentTypeColumn => false;

    public override bool ShowPeriodColumn => false;

    public override bool ShowDescriptionColumn => true;
}

public sealed class OfficialExamFeeViewModel : OccasionalFeeViewModel
{
    public OfficialExamFeeViewModel(
        ILogger<OfficialExamFeeViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
        : base(logger, scopedExecutor, dialogService, documentService, navigationService, currentUser, serviceProvider)
    {
    }

    public override string GenerateButtonText => "Bill Official Exam";

    protected override string GenerationDialogTitle => "Bill Official Exam";

    protected override PaymentTypeScope TypeScope => PaymentTypeScope.OfficialExam;

    protected override string ReportTitle => "Official Exam";

    public override bool ShowPaymentTypeColumn => false;

    public override bool ShowPeriodColumn => false;

    public override bool ShowDescriptionColumn => true;
}

/// <summary>
/// Shared listing for custom payment types (e.g. Excursion, Trip), with the
/// same Create bill flow as Livre.
/// </summary>
public sealed class CustomOneTimeFeeViewModel : OccasionalFeeViewModel
{
    public CustomOneTimeFeeViewModel(
        ILogger<CustomOneTimeFeeViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
        : base(logger, scopedExecutor, dialogService, documentService, navigationService, currentUser, serviceProvider)
    {
    }

    public override string GenerateButtonText => "Create bill";

    protected override string GenerationDialogTitle => "Create bill";

    protected override PaymentTypeScope TypeScope => PaymentTypeScope.CustomOneTime;

    protected override string ReportTitle => "Other fees";

    public override bool ShowPaymentTypeColumn => true;

    public override bool ShowPeriodColumn => false;

    public override bool ShowDescriptionColumn => true;
}
