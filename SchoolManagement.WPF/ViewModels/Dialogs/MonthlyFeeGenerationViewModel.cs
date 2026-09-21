using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.DTOs.SchoolYears;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Shared;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class SelectableMonth : ObservableObject
{
    private bool _isSelected;

    public SelectableMonth(int month, int year)
    {
        Month = month;
        Year = year;
        Label = Period.Label(month, year);
    }

    public int Month { get; }

    public int Year { get; }

    public string Label { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class MonthlyFeeGenerationViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private int? _preselectedStudentId;
    private string? _preselectedPaymentTypeName;

    private bool _billSingleStudent;
    private SchoolYearOption? _selectedSchoolYear;
    private StudentGroupOption? _selectedClass;
    private PaymentTypeOption? _selectedPaymentType;
    private decimal _amountPerMonth;
    private int _dueDay = 10;
    private bool _onlyActiveStudents = true;

    public MonthlyFeeGenerationViewModel(
        ILogger<MonthlyFeeGenerationViewModel> logger,
        IScopedExecutor scopedExecutor) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        Student = new StudentPicker(scopedExecutor);
        Title = "Bill monthly écolage";
        ConfirmButtonText = "Bill the selected months";
        DialogWidth = 640;
    }

    /// <summary>Opens the dialog already locked on one student (from the student list).</summary>
    public void ConfigureForStudent(int studentId, string? paymentTypeName = null)
    {
        _preselectedStudentId = studentId;
        _preselectedPaymentTypeName = paymentTypeName;
        BillSingleStudent = true;
        Title = string.IsNullOrWhiteSpace(paymentTypeName)
            ? "Bill monthly fee for one student"
            : $"Bill {paymentTypeName} for one student";
    }

    public StudentPicker Student { get; }

    public ObservableCollection<SchoolYearOption> SchoolYears { get; } = new();

    public ObservableCollection<StudentGroupOption> Classes { get; } = new();

    public ObservableCollection<PaymentTypeOption> PaymentTypes { get; } = new();

    public ObservableCollection<SelectableMonth> Months { get; } = new();

    public string ResultSummary { get; private set; } = string.Empty;

    public IReadOnlyList<int> CreatedFeeIds { get; private set; } = [];

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
        set
        {
            if (SetProperty(ref _selectedSchoolYear, value))
            {
                BuildMonths();
            }
        }
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
            if (SetProperty(ref _selectedPaymentType, value) && value is not null)
            {
                AmountPerMonth = value.DefaultAmount;
            }
        }
    }

    public decimal AmountPerMonth
    {
        get => _amountPerMonth;
        set => SetProperty(ref _amountPerMonth, value);
    }

    public int DueDay
    {
        get => _dueDay;
        set => SetProperty(ref _dueDay, value);
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
                    .ListOptionsAsync(PaymentFrequency.Monthly);

                return (yearOptions, groupOptions, typeOptions);
            });

            Fill(SchoolYears, years);
            Fill(Classes, groups);
            Fill(PaymentTypes, types);

            SelectedSchoolYear = years.FirstOrDefault(year => year.IsCurrent) ?? years.FirstOrDefault();
            SelectedPaymentType = !string.IsNullOrWhiteSpace(_preselectedPaymentTypeName)
                ? PaymentTypes.FirstOrDefault(type =>
                    string.Equals(type.Name, _preselectedPaymentTypeName, StringComparison.OrdinalIgnoreCase))
                  ?? PaymentTypes.FirstOrDefault()
                : PaymentTypes.FirstOrDefault();

            if (_preselectedStudentId is int studentId)
            {
                await Student.SelectAsync(studentId);
            }
        });

    protected override async Task<bool> SaveAsync()
    {
        if (SelectedPaymentType is null)
        {
            ErrorMessage = "Select the monthly payment type to bill.";
            return false;
        }

        if (AmountPerMonth <= 0)
        {
            ErrorMessage = "The monthly amount must be greater than zero.";
            return false;
        }

        if (DueDay is < 1 or > 28)
        {
            ErrorMessage = "The due day must be between 1 and 28.";
            return false;
        }

        var selected = Months.Where(month => month.IsSelected).ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Select at least one month.";
            return false;
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

        var year = selected[0].Year;
        var months = selected.Where(month => month.Year == year).Select(month => month.Month).ToList();

        var result = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IFeeService>().GenerateMonthlyAsync(
                new GenerateMonthlyFeesRequest(
                    SelectedPaymentType.Id,
                    AmountPerMonth,
                    months,
                    year,
                    DueDay,
                    AcademicLevelId: null,
                    StudentGroupId: studentGroupId,
                    StudentIds: studentIds,
                    OnlyActiveStudents: BillSingleStudent ? false : OnlyActiveStudents)));

        ResultSummary = result.ItemsCreated == 0
            ? "Nothing to bill: the selected months were already billed."
            : $"{result.ItemsCreated} fee lines created for {result.StudentsProcessed} student(s), "
              + $"total {Money.Format(result.TotalBilled)}"
              + (result.ItemsSkipped > 0 ? $" ({result.ItemsSkipped} already billed lines skipped)." : ".");

        CreatedFeeIds = result.CreatedFeeIds;
        return true;
    }

    private static void Fill<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void BuildMonths()
    {
        Months.Clear();

        if (SelectedSchoolYear is null)
        {
            return;
        }

        foreach (var (month, year) in Period.EnumerateMonths(SelectedSchoolYear.StartDate, SelectedSchoolYear.EndDate))
        {
            Months.Add(new SelectableMonth(month, year));
        }
    }
}
