using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.Payments;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.DTOs.SchoolYears;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

/// <summary>
/// Bills several fee types for one student and registers payment(s) immediately
/// (e.g. Droit + Écolage + Livre in one action). Partial amounts are allowed.
/// </summary>
public class CombinedBillAndPayViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly IDocumentService _documentService;

    private int _studentId;
    private string _studentName = string.Empty;
    private SchoolYearOption? _selectedSchoolYear;
    private DateTime? _paymentDate = DateTime.Today;
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private string? _reference;
    private bool _printReceipt = true;
    private int _dueDay = 10;

    public CombinedBillAndPayViewModel(
        ILogger<CombinedBillAndPayViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _documentService = documentService;

        Title = "Bill & pay several fees";
        ConfirmButtonText = "Bill and register payment";
        DialogWidth = 820;
    }

    public ObservableCollection<SchoolYearOption> SchoolYears { get; } = new();

    public ObservableCollection<CombinedBillLine> Lines { get; } = new();

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    public string StudentLabel { get; private set; } = string.Empty;

    public string ResultSummary { get; private set; } = string.Empty;

    public SchoolYearOption? SelectedSchoolYear
    {
        get => _selectedSchoolYear;
        set => SetProperty(ref _selectedSchoolYear, value);
    }

    public int DueDay
    {
        get => _dueDay;
        set => SetProperty(ref _dueDay, value);
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

    public bool PrintReceipt
    {
        get => _printReceipt;
        set => SetProperty(ref _printReceipt, value);
    }

    public decimal SelectedPayTotal => Lines.Where(line => line.IsSelected).Sum(line => line.PayAmount);

    public string SelectedPayTotalLabel => Money.Format(SelectedPayTotal);

    public void Initialize(int studentId, string studentName)
    {
        _studentId = studentId;
        _studentName = studentName;
        StudentLabel = studentName;
        Title = $"Bill & pay several fees — {studentName}";
        OnPropertyChanged(nameof(StudentLabel));
    }

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var (years, types, settingsDueDay) = await _scopedExecutor.RunAsync(async provider =>
            {
                var yearOptions = await provider.GetRequiredService<ISchoolYearService>().ListOptionsAsync();
                var typeOptions = await provider.GetRequiredService<IPaymentTypeService>().ListOptionsAsync();
                var settings = await provider.GetRequiredService<ISchoolSettingsService>().GetAsync();
                return (yearOptions, typeOptions, settings.DefaultDueDay);
            });

            DueDay = Math.Clamp(settingsDueDay, 1, 28);
            Fill(SchoolYears, years);
            SelectedSchoolYear = years.FirstOrDefault(year => year.IsCurrent) ?? years.FirstOrDefault();

            foreach (var line in Lines)
            {
                line.PropertyChanged -= OnLineChanged;
            }

            Lines.Clear();

            var today = DateTime.Today;
            foreach (var type in types.OrderBy(type => type.Name))
            {
                var line = new CombinedBillLine(type, today.Month, today.Year);
                line.PropertyChanged += OnLineChanged;
                Lines.Add(line);
            }

            OnPropertyChanged(nameof(SelectedPayTotal));
            OnPropertyChanged(nameof(SelectedPayTotalLabel));
        });

    protected override async Task<bool> SaveAsync()
    {
        var selected = Lines.Where(line => line.IsSelected).ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Select at least one fee type (e.g. Droit, Écolage, Livre).";
            return false;
        }

        if (SelectedSchoolYear is null)
        {
            ErrorMessage = "Select the school year.";
            return false;
        }

        if (PaymentDate is null)
        {
            ErrorMessage = "The payment date is required.";
            return false;
        }

        if (DueDay is < 1 or > 28)
        {
            ErrorMessage = "The due day for monthly fees must be between 1 and 28.";
            return false;
        }

        foreach (var line in selected)
        {
            if (line.BillAmount <= 0)
            {
                ErrorMessage = $"Enter a bill amount for {line.Type.Name}.";
                return false;
            }

            if (line.PayAmount < 0)
            {
                ErrorMessage = $"Payment amount for {line.Type.Name} cannot be negative.";
                return false;
            }

            if (line.PayAmount > line.BillAmount)
            {
                ErrorMessage = $"Payment for {line.Type.Name} cannot exceed the billed amount.";
                return false;
            }

            if (line.RequiresDescription && string.IsNullOrWhiteSpace(line.Description))
            {
                ErrorMessage = $"Enter a description for {line.Type.Name}.";
                return false;
            }
        }

        if (selected.All(line => line.PayAmount <= 0))
        {
            ErrorMessage = "Enter a payment amount on at least one selected fee (partial payments are allowed).";
            return false;
        }

        var schoolYearId = SelectedSchoolYear.Id;
        var dueDate = DateTime.Today.AddDays(30);
        var createdOrMatched = new List<PaymentAllocationLine>();
        var billedCount = 0;
        var skippedCount = 0;

        foreach (var line in selected)
        {
            FeeGenerationResult result;
            if (line.IsMonthly)
            {
                result = await _scopedExecutor.RunAsync(provider =>
                    provider.GetRequiredService<IFeeService>().GenerateMonthlyAsync(
                        new GenerateMonthlyFeesRequest(
                            line.Type.Id,
                            line.BillAmount,
                            [line.Month],
                            line.Year,
                            DueDay,
                            AcademicLevelId: null,
                            StudentGroupId: null,
                            StudentIds: [_studentId],
                            OnlyActiveStudents: false)));
            }
            else
            {
                result = await _scopedExecutor.RunAsync(provider =>
                    provider.GetRequiredService<IFeeService>().GenerateOneTimeAsync(
                        new GenerateOneTimeFeeRequest(
                            line.Type.Id,
                            line.Description,
                            line.BillAmount,
                            dueDate,
                            schoolYearId,
                            AcademicLevelId: null,
                            StudentGroupId: null,
                            StudentIds: [_studentId],
                            OnlyActiveStudents: false)));
            }

            billedCount += result.ItemsCreated;
            skippedCount += result.ItemsSkipped;

            int? feeId = result.CreatedFeeIds.Count > 0 ? result.CreatedFeeIds[0] : null;
            if (feeId is null)
            {
                feeId = await FindOutstandingFeeIdAsync(line);
            }

            if (feeId is int matchedFeeId && line.PayAmount > 0)
            {
                createdOrMatched.Add(new PaymentAllocationLine(matchedFeeId, line.PayAmount));
            }
        }

        if (createdOrMatched.Count == 0)
        {
            ResultSummary = billedCount == 0
                ? "Nothing to bill or pay: selected fees already exist and have no outstanding balance."
                : $"{billedCount} fee line(s) created, but no payment amount was applied.";
            return true;
        }

        var multi = new RegisterMultiPaymentRequest(
            _studentId,
            createdOrMatched,
            PaymentDate.Value,
            PaymentMethod,
            Reference,
            Notes: $"Combined bill & pay for {_studentName}");

        var paymentResult = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().RegisterManyAsync(multi));

        ResultSummary =
            $"{billedCount} fee line(s) created"
            + (skippedCount > 0 ? $" ({skippedCount} already billed skipped)" : string.Empty)
            + $". {paymentResult.Payments.Count} payment(s) registered for {Money.Format(paymentResult.TotalApplied)}"
            + (paymentResult.PrimaryReceiptNumber is null
                ? "."
                : $". Receipt {paymentResult.PrimaryReceiptNumber}.");

        if (PrintReceipt && paymentResult.PrimaryReceiptId is int receiptId)
        {
            try
            {
                await _documentService.PrintReceiptAsync(receiptId);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Combined payment receipt could not be printed");
                _dialogService.ShowError(
                    "Fees and payments are saved but a receipt could not be printed. "
                    + "Reprint it from the receipts screen.",
                    "Printing");
            }
        }

        return true;
    }

    private async Task<int?> FindOutstandingFeeIdAsync(CombinedBillLine line)
    {
        var outstanding = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IFeeService>().ListOutstandingByStudentAsync(_studentId));

        IEnumerable<StudentFeeItem> matches = outstanding
            .Where(fee => fee.PaymentTypeId == line.Type.Id);

        if (line.IsMonthly)
        {
            matches = matches.Where(fee => fee.Month == line.Month && fee.Year == line.Year);
        }
        else if (line.RequiresDescription)
        {
            var description = line.Description.Trim();
            matches = matches.Where(fee =>
                string.Equals(fee.Description?.Trim(), description, StringComparison.OrdinalIgnoreCase));
        }

        return matches
            .OrderBy(fee => fee.DueDate)
            .Select(fee => (int?)fee.StudentFeeId)
            .FirstOrDefault();
    }

    private void OnLineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CombinedBillLine.IsSelected) or nameof(CombinedBillLine.PayAmount))
        {
            OnPropertyChanged(nameof(SelectedPayTotal));
            OnPropertyChanged(nameof(SelectedPayTotalLabel));
        }
    }

    private static void Fill<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}

public sealed class CombinedBillLine : ObservableObject
{
    private bool _isSelected;
    private decimal _billAmount;
    private decimal _payAmount;
    private string _description = string.Empty;
    private int _month;
    private int _year;

    public CombinedBillLine(PaymentTypeOption type, int month, int year)
    {
        Type = type;
        RequiresDescription = WellKnownPaymentTypes.RequiresDistinctDescription(type.Name)
            && type.Frequency != PaymentFrequency.Monthly;
        IsMonthly = type.Frequency == PaymentFrequency.Monthly;
        _billAmount = type.DefaultAmount;
        _payAmount = type.DefaultAmount;
        _description = type.Name;
        _month = month;
        _year = year;
    }

    public PaymentTypeOption Type { get; }

    public bool RequiresDescription { get; }

    public bool IsMonthly { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
            {
                return;
            }

            if (value && PayAmount <= 0)
            {
                PayAmount = BillAmount;
            }
        }
    }

    public decimal BillAmount
    {
        get => _billAmount;
        set
        {
            if (!SetProperty(ref _billAmount, value))
            {
                return;
            }

            if (IsSelected)
            {
                PayAmount = value;
            }
        }
    }

    public decimal PayAmount
    {
        get => _payAmount;
        set => SetProperty(ref _payAmount, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public int Month
    {
        get => _month;
        set => SetProperty(ref _month, value);
    }

    public int Year
    {
        get => _year;
        set => SetProperty(ref _year, value);
    }

    public string MonthLabel => IsMonthly ? Period.Label(Month, Year) : string.Empty;
}
