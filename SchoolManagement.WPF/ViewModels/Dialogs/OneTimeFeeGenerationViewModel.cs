using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.Payments;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.DTOs.SchoolYears;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Shared;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class OneTimeFeeGenerationViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService? _dialogService;
    private readonly IDocumentService? _documentService;
    private PaymentTypeScope _scope = PaymentTypeScope.OneTime;
    private int? _preselectedStudentId;

    private bool _billSingleStudent;
    private bool _immediatePaymentMode;
    private SchoolYearOption? _selectedSchoolYear;
    private StudentGroupOption? _selectedClass;
    private PaymentTypeOption? _selectedPaymentType;
    private string _description = string.Empty;
    private decimal _amount;
    private decimal _paymentAmount;
    private DateTime? _dueDate = DateTime.Today.AddDays(30);
    private DateTime? _paymentDate = DateTime.Today;
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private bool _printReceipt = true;
    private bool _onlyActiveStudents = true;

    public OneTimeFeeGenerationViewModel(
        ILogger<OneTimeFeeGenerationViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        IDocumentService documentService) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _documentService = documentService;
        Student = new StudentPicker(scopedExecutor);
        Title = "Bill a one-time fee";
        ConfirmButtonText = "Bill the fee";
        DialogWidth = 640;
    }

    public void Configure(PaymentTypeScope scope, string title)
    {
        _scope = scope;
        Title = title;
        ImmediatePaymentMode = false;
        ConfirmButtonText = "Bill the fee";
        RefreshDescriptionMode(scope.ExactName);
    }

    /// <summary>Student-list Create bill: create the fee and register payment in one step.</summary>
    public void ConfigureForStudent(PaymentTypeScope scope, string title, int studentId)
    {
        Configure(scope, title);
        _preselectedStudentId = studentId;
        BillSingleStudent = true;
        ImmediatePaymentMode = true;
        ConfirmButtonText = "Bill and register payment";
        Title = title.StartsWith("Create bill", StringComparison.OrdinalIgnoreCase) || title.StartsWith("Bill ", StringComparison.OrdinalIgnoreCase)
            ? title
            : $"Bill and pay — {title}";
    }

    public bool RequiresDescription { get; private set; }

    public bool ImmediatePaymentMode
    {
        get => _immediatePaymentMode;
        private set
        {
            if (SetProperty(ref _immediatePaymentMode, value))
            {
                OnPropertyChanged(nameof(BillingHint));
            }
        }
    }

    public string BillingHint => ImmediatePaymentMode
        ? (RequiresDescription
            ? "Creates the fee and registers the payment now. Use a distinct description for each line. Payment amount may be partial."
            : "Creates the fee and registers the payment now. Payment amount may be partial.")
        : RequiresDescription
            ? "Creates a fee for the selected school year. Use a distinct description for each line; the same description in the same year is skipped."
            : "Creates one fee per student for the selected school year. Students who already have this fee in that year are skipped.";

    public string DescriptionLabel => RequiresDescription
        ? (WellKnownPaymentTypes.IsLivre(_scope.ExactName ?? SelectedPaymentType?.Name ?? string.Empty)
            ? "Book description"
            : "Description")
        : "Description";

    public StudentPicker Student { get; }

    public ObservableCollection<SchoolYearOption> SchoolYears { get; } = new();

    public ObservableCollection<StudentGroupOption> Classes { get; } = new();

    public ObservableCollection<PaymentTypeOption> PaymentTypes { get; } = new();

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    public string ResultSummary { get; private set; } = string.Empty;

    public bool BillSingleStudent
    {
        get => _billSingleStudent;
        set
        {
            if (SetProperty(ref _billSingleStudent, value))
            {
                OnPropertyChanged(nameof(BillEntireGroup));
            }
        }
    }

    public bool BillEntireGroup
    {
        get => !_billSingleStudent;
        set
        {
            if (value)
            {
                BillSingleStudent = false;
            }
        }
    }

    public SchoolYearOption? SelectedSchoolYear
    {
        get => _selectedSchoolYear;
        set => SetProperty(ref _selectedSchoolYear, value);
    }

    public StudentGroupOption? SelectedClass
    {
        get => _selectedClass;
        set => SetProperty(ref _selectedClass, value);
    }

    public PaymentTypeOption? SelectedPaymentType
    {
        get => _selectedPaymentType;
        set
        {
            if (!SetProperty(ref _selectedPaymentType, value) || value is null)
            {
                return;
            }

            RefreshDescriptionMode(value.Name);
            Amount = value.DefaultAmount;

            if (string.IsNullOrWhiteSpace(Description))
            {
                Description = value.Name;
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                PaymentAmount = value;
            }
        }
    }

    public decimal PaymentAmount
    {
        get => _paymentAmount;
        set => SetProperty(ref _paymentAmount, value);
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
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

    public bool PrintReceipt
    {
        get => _printReceipt;
        set => SetProperty(ref _printReceipt, value);
    }

    public bool OnlyActiveStudents
    {
        get => _onlyActiveStudents;
        set => SetProperty(ref _onlyActiveStudents, value);
    }

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var (years, groups, types) = await _scopedExecutor.RunAsync(async provider =>
            {
                var yearOptions = await provider.GetRequiredService<ISchoolYearService>().ListOptionsAsync();
                var groupOptions = await provider.GetRequiredService<IStudentGroupService>().ListOptionsAsync();
                var typeOptions = await provider.GetRequiredService<IPaymentTypeService>()
                    .ListOptionsAsync(_scope);
                return (yearOptions, groupOptions, typeOptions);
            });

            Fill(SchoolYears, years);
            Fill(Classes, groups);
            Fill(PaymentTypes, types);
            SelectedSchoolYear = years.FirstOrDefault(year => year.IsCurrent) ?? years.FirstOrDefault();
            SelectedPaymentType = PaymentTypes.FirstOrDefault();

            if (_preselectedStudentId is int studentId)
            {
                await Student.SelectAsync(studentId);
            }
        });

    protected override async Task<bool> SaveAsync()
    {
        if (SelectedSchoolYear is null)
        {
            ErrorMessage = "Select the school year.";
            return false;
        }

        if (SelectedPaymentType is null)
        {
            ErrorMessage = "Select the payment type to bill.";
            return false;
        }

        if (Amount <= 0)
        {
            ErrorMessage = "The amount must be greater than zero.";
            return false;
        }

        if (DueDate is null)
        {
            ErrorMessage = "The due date is required.";
            return false;
        }

        if (RequiresDescription && string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Enter a description so this line can be told apart from others.";
            return false;
        }

        if (ImmediatePaymentMode)
        {
            if (PaymentAmount <= 0)
            {
                ErrorMessage = "The payment amount must be greater than zero.";
                return false;
            }

            if (PaymentAmount > Amount)
            {
                ErrorMessage = "The payment amount cannot exceed the billed amount.";
                return false;
            }

            if (PaymentDate is null)
            {
                ErrorMessage = "The payment date is required.";
                return false;
            }
        }

        IReadOnlyList<int>? studentIds = null;
        int? studentGroupId = null;

        if (BillSingleStudent)
        {
            if (Student.SelectedStudent is null)
            {
                ErrorMessage = "Search and select the student to bill.";
                return false;
            }

            studentIds = [Student.SelectedStudent.Id];
        }
        else
        {
            studentGroupId = SelectedClass?.Id;
        }

        var result = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IFeeService>().GenerateOneTimeAsync(
                new GenerateOneTimeFeeRequest(
                    SelectedPaymentType.Id,
                    Description,
                    Amount,
                    DueDate.Value,
                    SelectedSchoolYear.Id,
                    AcademicLevelId: null,
                    StudentGroupId: studentGroupId,
                    StudentIds: studentIds,
                    OnlyActiveStudents: BillSingleStudent ? false : OnlyActiveStudents)));

        if (result.ItemsCreated == 0)
        {
            ResultSummary = "Nothing to bill: this fee was already billed for the selected school year.";
            return true;
        }

        ResultSummary = $"{result.ItemsCreated} fee lines created for {result.StudentsProcessed} student(s), "
            + $"total {Money.Format(result.TotalBilled)}.";

        if (!ImmediatePaymentMode || result.CreatedFeeIds.Count == 0 || Student.SelectedStudent is null)
        {
            return true;
        }

        var feeId = result.CreatedFeeIds[0];
        var payRequest = new RegisterPaymentRequest(
            Student.SelectedStudent.Id,
            feeId,
            PaymentAmount,
            PaymentDate!.Value,
            PaymentMethod,
            Reference: null,
            Notes: Description);

        var payment = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IPaymentService>().RegisterAsync(payRequest));

        ResultSummary +=
            $" Payment {payment.PaymentNumber} of {Money.Format(payment.AmountApplied)} registered"
            + $" (receipt {payment.ReceiptNumber}). Remaining: {Money.Format(payment.FeeRemainingAmount)}.";

        if (PrintReceipt && _documentService is not null)
        {
            try
            {
                await _documentService.PrintReceiptAsync(payment.ReceiptId);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Receipt {Receipt} could not be printed", payment.ReceiptNumber);
                _dialogService?.ShowError(
                    $"The fee and payment are saved but receipt {payment.ReceiptNumber} could not be printed.",
                    "Printing");
            }
        }

        return true;
    }

    private void RefreshDescriptionMode(string? paymentTypeName)
    {
        RequiresDescription = WellKnownPaymentTypes.RequiresDistinctDescription(paymentTypeName ?? string.Empty);
        OnPropertyChanged(nameof(RequiresDescription));
        OnPropertyChanged(nameof(BillingHint));
        OnPropertyChanged(nameof(DescriptionLabel));
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
