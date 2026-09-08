using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Billing;

/// <summary>
/// Late and partially settled fees across every payment type, with the action
/// that recomputes the overdue flags against today's date.
/// </summary>
public class UnpaidFeeViewModel : FeeListViewModelBase
{
    public UnpaidFeeViewModel(
        ILogger<UnpaidFeeViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
        : base(logger, scopedExecutor, dialogService, documentService, navigationService, currentUser, serviceProvider)
    {
        RefreshOverdueCommand = new AsyncRelayCommand(
            RefreshOverdueAsync,
            () => currentUser.HasPermission(Permission.ManageFees));
    }

    public ICommand RefreshOverdueCommand { get; }

    protected override PaymentTypeScope TypeScope => PaymentTypeScope.All;

    protected override string ReportTitle => "Unpaid fees";

    protected override bool DefaultToOutstandingOnly => true;

    private async Task RefreshOverdueAsync()
    {
        var updated = 0;

        var succeeded = await RunGuardedAsync(async () =>
        {
            updated = await ScopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IFeeService>().RefreshOverdueStatusesAsync());
        });

        if (!succeeded)
        {
            return;
        }

        await ReloadAsync();
        StatusMessage = updated == 0
            ? "No new overdue fee was found."
            : $"{updated} fee lines have been marked as overdue.";
    }
}
