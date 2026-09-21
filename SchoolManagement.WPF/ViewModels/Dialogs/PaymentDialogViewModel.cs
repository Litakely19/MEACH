using System.Collections.ObjectModel;
using System.ComponentModel;
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
/// Registers money against one or several StudentFee lines and issues receipts.
/// </summary>
public class PaymentDialogViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly IDocumentService _documentService;

    private int? _initialStudentId;
    private int? _initialStudentFeeId;
    private IReadOnlyList<int>? _preselectedFeeIds;
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
        DialogWidth = 780;

        Student = new StudentPicker(scopedExecutor);
        Student.SelectedStudentChanged += (_, _) => _ = LoadFeesAsync();
    }

    public StudentPicker Student { get; }

    public ObservableCollection<FeePaymentLine> FeeLines { get; } = new();

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    public string ResultSummary { get; private set; } = string.Empty;

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

    public decimal SelectedTotal => FeeLines.Where(line => line.IsSelected).Sum(line => line.Amount);

    public string SelectedTotalLabel => Money.Format(SelectedTotal);

    public void Initialize(int? studentId = null, int? studentFeeId = null)
    {
        _initialStudentId = studentId;
        _initialStudentFeeId = studentFeeId;
        _preselectedFeeIds = studentFeeId is null ? null : [studentFeeId.Value];
    }

    public void InitializeForFees(int studentId, IReadOnlyList<int> studentFeeIds)
    {
        _initialStudentId = studentId;
        _initialStudentFeeId = studentFeeIds.FirstOrDefault();
        _preselectedFeeIds = studentFeeIds;
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

        var selected = FeeLines.Where(line => line.IsSelected && line.Amount > 0).ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Select at least one obligation and enter an amount.";
            return false;
        }

        foreach (var line in selected)
        {
            if (line.Amount > line.Fee.RemainingAmount)
            {
                ErrorMessage =
                    $"Amount for {line.Fee.PeriodLabel} exceeds the remaining balance ({Money.Format(line.Fee.RemainingAmount)}).";
                return false;
            }
        }

        if (PaymentDate is null)
        {
            ErrorMessage = "The payment date is required.";
            return false;
        }

        var multiRequest = new RegisterMultiPaymentRequest(
            Student.SelectedStudent.Id,
            selected.Select(line => new PaymentAllocationLine(line.Fee.StudentFeeId, line.Amount)).ToList(),
            PaymentDate.Value,
            PaymentMethod,
            Reference,
            Notes);

        var probe = new RegisterPaymentRequest(
            multiRequest.StudentId,
            selected[0].Fee.StudentFeeId,
            selected.Sum(line => line.Amount),
            PaymentDate.Value,
            PaymentMethod,
            Reference,
            Notes);

        var duplicate = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().CheckForDuplicateAsync(probe));

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

            multiRequest = multiRequest with { DuplicateConfirmed = true };
        }

        var result = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().RegisterManyAsync(multiRequest));

        ResultSummary = selected.Count == 1
            ? $"Payment {result.Payments[0].PaymentNumber} of {Money.Format(result.TotalApplied)} registered, "
              + $"receipt {result.Payments[0].ReceiptNumber}. Remaining: {Money.Format(result.Payments[0].FeeRemainingAmount)}."
            : $"{result.Payments.Count} payments registered for a total of {Money.Format(result.TotalApplied)}"
              + (result.PrimaryReceiptNumber is null ? "." : $". First receipt: {result.PrimaryReceiptNumber}.");

        if (PrintReceipt && result.PrimaryReceiptId is int receiptId)
        {
            try
            {
                await _documentService.PrintReceiptAsync(receiptId);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "A receipt could not be printed after multi-payment");
                _dialogService.ShowError(
                    "The payment is registered but a receipt could not be printed. "
                    + "Reprint it from the receipts screen.",
                    "Printing");
            }
        }

        return true;
    }

    private Task LoadFeesAsync() => RunGuardedAsync(LoadFeesCoreAsync);

    private async Task LoadFeesCoreAsync()
    {
        foreach (var line in FeeLines)
        {
            line.PropertyChanged -= OnFeeLineChanged;
        }

        FeeLines.Clear();
        OnPropertyChanged(nameof(SelectedTotal));
        OnPropertyChanged(nameof(SelectedTotalLabel));

        if (Student.SelectedStudent is null)
        {
            return;
        }

        var fees = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IFeeService>()
                .ListOutstandingByStudentAsync(Student.SelectedStudent.Id));

        var preselect = _preselectedFeeIds?.ToHashSet() ?? [];

        foreach (var fee in fees)
        {
            var selected = preselect.Count == 0
                ? fee.StudentFeeId == (_initialStudentFeeId ?? fees.FirstOrDefault()?.StudentFeeId)
                : preselect.Contains(fee.StudentFeeId);

            var line = new FeePaymentLine(fee, selected);
            line.PropertyChanged += OnFeeLineChanged;
            FeeLines.Add(line);
        }

        if (FeeLines.Count == 0)
        {
            StatusMessage = "This student has no outstanding balance.";
        }

        OnPropertyChanged(nameof(SelectedTotal));
        OnPropertyChanged(nameof(SelectedTotalLabel));
    }

    private void OnFeeLineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FeePaymentLine.IsSelected) or nameof(FeePaymentLine.Amount))
        {
            OnPropertyChanged(nameof(SelectedTotal));
            OnPropertyChanged(nameof(SelectedTotalLabel));
        }
    }
}

public sealed class FeePaymentLine : ObservableObject
{
    private bool _isSelected;
    private decimal _amount;

    public FeePaymentLine(StudentFeeItem fee, bool isSelected)
    {
        Fee = fee;
        _isSelected = isSelected;
        _amount = isSelected ? fee.RemainingAmount : 0m;
    }

    public StudentFeeItem Fee { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
            {
                return;
            }

            if (value && Amount <= 0)
            {
                Amount = Fee.RemainingAmount;
            }
        }
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }
}
