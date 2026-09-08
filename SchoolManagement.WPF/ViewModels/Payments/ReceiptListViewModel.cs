using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Receipts;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.Payments;

/// <summary>
/// Issued receipts, with reprint and PDF export. Every print is counted, so a
/// reprinted receipt is identifiable.
/// </summary>
public class ReceiptListViewModel : PagedListViewModel<ReceiptListItem>
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private DateTime? _from = DateTime.Today.AddDays(-30);
    private DateTime? _to = DateTime.Today;

    public ReceiptListViewModel(
        ILogger<ReceiptListViewModel> logger,
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

        PrintCommand = new AsyncRelayCommand(PrintAsync, () => SelectedItem is not null && CanIssue);
        ExportPdfCommand = new AsyncRelayCommand(ExportAsync, () => SelectedItem is not null);
        OpenPaymentCommand = new AsyncRelayCommand(OpenPaymentAsync, () => SelectedItem is not null);
    }

    public bool CanIssue => _currentUser.HasPermission(Permission.IssueReceipts);

    public ICommand PrintCommand { get; }

    public ICommand ExportPdfCommand { get; }

    public ICommand OpenPaymentCommand { get; }

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

    protected override Task<PagedResult<ReceiptListItem>> FetchAsync(int page) =>
        _scopedExecutor.RunAsync(provider => provider.GetRequiredService<IReceiptService>().ListAsync(
            new ReceiptFilter(SearchTerm, StudentId: null, From, To, page, PageSize)));

    protected override Task OnPageLoadedAsync()
    {
        RaiseSelectionCommands();
        return Task.CompletedTask;
    }

    protected override void OnSelectionChanged() => RaiseSelectionCommands();

    private Task PrintAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        var receipt = SelectedItem;

        return RunGuardedAsync(async () =>
        {
            if (!await _documentService.PrintReceiptAsync(receipt.Id))
            {
                return;
            }

            await ReloadCurrentPageCoreAsync();
            StatusMessage = $"Receipt {receipt.ReceiptNumber} has been sent to the printer.";
        });
    }

    private Task ExportAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        var receiptId = SelectedItem.Id;

        return RunGuardedAsync(async () =>
        {
            var path = await _documentService.ExportReceiptAsync(receiptId);

            if (path is not null)
            {
                StatusMessage = $"Receipt written to {path}";
            }
        });
    }

    private async Task OpenPaymentAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<PaymentDetailViewModel>();
        viewModel.Initialize(SelectedItem.PaymentId);

        await _dialogService.ShowDialogAsync(viewModel);
        await ReloadCurrentPageAsync();
    }

    private void RaiseSelectionCommands()
    {
        (PrintCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ExportPdfCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenPaymentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
