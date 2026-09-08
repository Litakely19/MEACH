using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Payments;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

/// <summary>
/// Payment file: what was received, how it was allocated to the invoice lines and
/// the receipt it produced. Cancellation and reversal keep the row and undo the
/// balances, so the financial history stays auditable.
/// </summary>
public class PaymentDetailViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _currentUser;

    private int _paymentId;
    private PaymentDetail? _payment;

    public PaymentDetailViewModel(
        ILogger<PaymentDetailViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService,
        ICurrentUserService currentUser) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _documentService = documentService;
        _currentUser = currentUser;

        Title = "Payment";
        IsEditable = false;
        DialogWidth = 700;

        PrintReceiptCommand = new AsyncRelayCommand(PrintReceiptAsync, () => Payment?.ReceiptId is not null);
        ExportReceiptCommand = new AsyncRelayCommand(ExportReceiptAsync, () => Payment?.ReceiptId is not null);
        CancelPaymentCommand = new AsyncRelayCommand(() => CloseOutAsync(reverse: false), CanCloseOut);
        ReversePaymentCommand = new AsyncRelayCommand(() => CloseOutAsync(reverse: true), CanCloseOut);
    }

    public ICommand PrintReceiptCommand { get; }

    public ICommand ExportReceiptCommand { get; }

    public ICommand CancelPaymentCommand { get; }

    public ICommand ReversePaymentCommand { get; }

    public PaymentDetail? Payment
    {
        get => _payment;
        private set => SetProperty(ref _payment, value);
    }

    public bool IsCancelled => Payment?.Status is PaymentStatus.Cancelled or PaymentStatus.Reversed;

    public void Initialize(int paymentId) => _paymentId = paymentId;

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var payment = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IPaymentService>().GetDetailAsync(_paymentId));

            Payment = payment;
            Title = $"Payment {payment.PaymentNumber}";

            OnPropertyChanged(nameof(IsCancelled));
            RaiseCommands();
        });

    protected override Task<bool> SaveAsync() => Task.FromResult(true);

    private bool CanCloseOut() =>
        _currentUser.HasPermission(Permission.CancelPayments) && Payment?.Status == PaymentStatus.Active;

    private async Task CloseOutAsync(bool reverse)
    {
        if (Payment is null)
        {
            return;
        }

        var action = reverse ? "Reverse" : "Cancel";

        if (!_dialogService.ConfirmCritical(
                $"{action} payment {Payment.PaymentNumber}?\n\n"
                + "The amount will be removed from the invoice balances. The payment stays visible in the history.",
                $"{action} a payment"))
        {
            return;
        }

        var reason = _dialogService.AskForText(
            $"Reason for the {(reverse ? "reversal" : "cancellation")}:",
            $"{action} a payment");

        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        var applied = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().CancelAsync(
                new CancelPaymentRequest(Payment.Id, reason, reverse))));

        if (!applied)
        {
            return;
        }

        await LoadAsync();
        StatusMessage = reverse ? "The payment has been reversed." : "The payment has been cancelled.";
    }

    private Task PrintReceiptAsync() =>
        RunGuardedAsync(async () =>
        {
            if (Payment?.ReceiptId is null)
            {
                return;
            }

            await _documentService.PrintReceiptAsync(Payment.ReceiptId.Value);
            StatusMessage = "The receipt has been sent to the printer.";
        });

    private Task ExportReceiptAsync() =>
        RunGuardedAsync(async () =>
        {
            if (Payment?.ReceiptId is null)
            {
                return;
            }

            var path = await _documentService.ExportReceiptAsync(Payment.ReceiptId.Value);

            if (path is not null)
            {
                StatusMessage = $"Receipt written to {path}";
            }
        });

    private void RaiseCommands()
    {
        (PrintReceiptCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ExportReceiptCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CancelPaymentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ReversePaymentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
