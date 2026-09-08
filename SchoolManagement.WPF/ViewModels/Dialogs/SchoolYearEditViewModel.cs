using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.SchoolYears;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class SchoolYearEditViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int? _schoolYearId;
    private string _name = string.Empty;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private bool _setAsCurrent;

    public SchoolYearEditViewModel(ILogger<SchoolYearEditViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        DialogWidth = 480;
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public bool SetAsCurrent
    {
        get => _setAsCurrent;
        set => SetProperty(ref _setAsCurrent, value);
    }

    public void InitializeForCreate()
    {
        _schoolYearId = null;
        Title = "New school year";
        ConfirmButtonText = "Create";

        // A Malagasy school year runs from September to July of the following year.
        var startYear = DateTime.Today.Month >= 8 ? DateTime.Today.Year : DateTime.Today.Year - 1;
        Name = $"{startYear}-{startYear + 1}";
        StartDate = new DateTime(startYear, 9, 1);
        EndDate = new DateTime(startYear + 1, 7, 31);
        SetAsCurrent = true;
    }

    public void InitializeForEdit(SchoolYearListItem schoolYear)
    {
        _schoolYearId = schoolYear.Id;
        Title = $"Edit school year {schoolYear.Name}";
        ConfirmButtonText = "Save";

        Name = schoolYear.Name;
        StartDate = schoolYear.StartDate;
        EndDate = schoolYear.EndDate;
        SetAsCurrent = schoolYear.IsCurrent;
    }

    protected override async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "The school year name is required, for instance 2026-2027.";
            return false;
        }

        if (StartDate is null || EndDate is null)
        {
            ErrorMessage = "The start date and the end date are required.";
            return false;
        }

        if (EndDate <= StartDate)
        {
            ErrorMessage = "The end date must come after the start date.";
            return false;
        }

        await _scopedExecutor.RunAsync(async provider =>
        {
            var service = provider.GetRequiredService<ISchoolYearService>();

            if (_schoolYearId is null)
            {
                await service.CreateAsync(new CreateSchoolYearRequest(
                    Name,
                    StartDate.Value,
                    EndDate.Value,
                    SetAsCurrent));

                return;
            }

            await service.UpdateAsync(new UpdateSchoolYearRequest(
                _schoolYearId.Value,
                Name,
                StartDate.Value,
                EndDate.Value,
                SetAsCurrent));
        });

        return true;
    }
}
