using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.Payments;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Shared;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

/// <summary>
/// Registers money against one StudentFee and issues the receipt in the same operation.
/// </summary>
public class PaymentDialogViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly IDocumentService _documentService;

    private int? _initialStudentId;
    private int? _initialStudentFeeId;
    private StudentFeeItem? _selectedFee;
    private decimal _amount;
    private DateTime? _paymentDate = DateTime.Today;
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private string? _reference;
    private string? _notes;
    private bool _printReceipt = true;

    public PaymentDialogViewModel(
        ILogger<PaymentDialogViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _documentService = documentService;

        Title = "Register a payment";
        ConfirmButtonText = "Register the payment";
        DialogWidth = 720;

        Student = new StudentPicker(scopedExecutor);
        Student.SelectedStudentChanged += (_, _) => _ = LoadFeesAsync();
    }

    public StudentPicker Student { get; }

    public ObservableCollection<StudentFeeItem> OutstandingFees { get; } = new();

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    public string ResultSummary { get; private set; } = string.Empty;

    public StudentFeeItem? SelectedFee
    {
        get => _selectedFee;
        set
        {
            if (!SetProperty(ref _selectedFee, value))
            {
                return;
            }

            OnPropertyChanged(nameof(RemainingLabel));
            OnPropertyChanged(nameof(ObligationLabel));

            if (value is not null)
            {
                Amount = value.RemainingAmount;
            }
        }
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public DateTime? PaymentDate
    {
        get => _paymentDate;
        set => SetProperty(ref _paymentDate, value);
    }

    public PaymentMethod PaymentMethod
    {
        get => _paymentMethod;
        set => SetProperty(ref _paymentMethod, value);
    }

    public string? Reference
    {
        get => _reference;
        set => SetProperty(ref _reference, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool PrintReceipt
    {
        get => _printReceipt;
        set => SetProperty(ref _printReceipt, value);
    }

    public string RemainingLabel => SelectedFee is null ? "-" : Money.Format(SelectedFee.RemainingAmount);

    public string ObligationLabel => SelectedFee is null
        ? "-"
        : $"{SelectedFee.PeriodLabel} — expected {Money.Format(SelectedFee.ExpectedAmount)}, paid {Money.Format(SelectedFee.PaidAmount)}";

    public void Initialize(int? studentId = null, int? studentFeeId = null)
    {
        _initialStudentId = studentId;
        _initialStudentFeeId = studentFeeId;
    }

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            if (_initialStudentId is not null)
            {
                await Student.SelectAsync(_initialStudentId.Value);
                await LoadFeesCoreAsync();
            }
        });

    protected override async Task<bool> SaveAsync()
    {
        if (Student.SelectedStudent is null)
        {
            ErrorMessage = "Select the student who is paying.";
            return false;
        }

        if (SelectedFee is null)
        {
            ErrorMessage = "Select the payment obligation this payment settles.";
            return false;
        }

        if (Amount <= 0)
        {
            ErrorMessage = "The amount must be greater than zero.";
            return false;
        }

        if (Amount > SelectedFee.RemainingAmount)
        {
            ErrorMessage = $"The amount exceeds the remaining balance ({RemainingLabel}).";
            return false;
        }

        if (PaymentDate is null)
        {
            ErrorMessage = "The payment date is required.";
            return false;
        }

        var request = new RegisterPaymentRequest(
            Student.SelectedStudent.Id,
            SelectedFee.StudentFeeId,
            Amount,
            PaymentDate.Value,
            PaymentMethod,
            Reference,
            Notes);

        var duplicate = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().CheckForDuplicateAsync(request));

        if (duplicate is not null)
        {
            var confirmed = _dialogService.ConfirmCritical(
                $"Payment {duplicate.ExistingPaymentNumber} of {Money.Format(duplicate.Amount)} was already "
                + $"registered on {duplicate.PaymentDate:d} by {duplicate.ReceivedBy}.\n\n"
                + "Register this payment anyway?",
                "Possible duplicate");

            if (!confirmed)
            {
                return false;
            }

            request = request with { DuplicateConfirmed = true };
        }

        var result = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().RegisterAsync(request));

        ResultSummary = $"Payment {result.PaymentNumber} of {Money.Format(result.AmountApplied)} registered, "
            + $"receipt {result.ReceiptNumber}. Remaining: {Money.Format(result.FeeRemainingAmount)}.";

        if (PrintReceipt)
        {
            try
            {
                await _documentService.PrintReceiptAsync(result.ReceiptId);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "The receipt {Receipt} could not be printed", result.ReceiptNumber);
                _dialogService.ShowError(
                    $"The payment is registered but receipt {result.ReceiptNumber} could not be printed. "
                    + "Reprint it from the receipts screen.",
                    "Printing");
            }
        }

        return true;
    }

    private Task LoadFeesAsync() => RunGuardedAsync(LoadFeesCoreAsync);

    private async Task LoadFeesCoreAsync()
    {
        OutstandingFees.Clear();
        SelectedFee = null;

        if (Student.SelectedStudent is null)
        {
            return;
        }

        var fees = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IFeeService>()
                .ListOutstandingByStudentAsync(Student.SelectedStudent.Id));

        foreach (var fee in fees)
        {
            OutstandingFees.Add(fee);
        }

        SelectedFee = _initialStudentFeeId is null
            ? OutstandingFees.FirstOrDefault()
            : OutstandingFees.FirstOrDefault(fee => fee.StudentFeeId == _initialStudentFeeId)
              ?? OutstandingFees.FirstOrDefault();

        if (OutstandingFees.Count == 0)
        {
            StatusMessage = "This student has no outstanding balance.";
        }
    }
}
