using System.Collections.ObjectModel;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using SchoolManagement.Application.DTOs.AcademicLevels;

using SchoolManagement.Application.DTOs.SchoolYears;

using SchoolManagement.Application.DTOs.StudentGroups;

using SchoolManagement.Application.DTOs.Students;

using SchoolManagement.Application.Interfaces;

using SchoolManagement.Domain.Enums;

using SchoolManagement.WPF.Mvvm;

using SchoolManagement.WPF.Services;



namespace SchoolManagement.WPF.ViewModels.Dialogs;



public class StudentEditViewModel : DialogViewModelBase

{

    private readonly IScopedExecutor _scopedExecutor;



    private int? _studentId;

    private string _studentNumberLabel = "Assigned on save";

    private string _firstName = string.Empty;

    private string _lastName = string.Empty;

    private Gender _gender = Gender.Male;

    private DateTime? _dateOfBirth = new DateTime(DateTime.Today.Year - 18, 1, 1);

    private string? _address;

    private string? _phoneNumber;

    private string? _email;

    private AcademicLevelOption? _selectedLevel;

    private StudentGroupOption? _selectedGroup;

    private SchoolYearOption? _selectedSchoolYear;

    private DateTime? _enrollmentDate = DateTime.Today;

    private StudentStatus _status = StudentStatus.Active;

    private bool _lookupsReady;



    public StudentEditViewModel(ILogger<StudentEditViewModel> logger, IScopedExecutor scopedExecutor)

        : base(logger)

    {

        _scopedExecutor = scopedExecutor;

        DialogWidth = 720;

    }



    public ObservableCollection<AcademicLevelOption> Levels { get; } = new();



    public ObservableCollection<StudentGroupOption> Groups { get; } = new();



    public ObservableCollection<SchoolYearOption> SchoolYears { get; } = new();



    public IReadOnlyList<Gender> Genders { get; } = Enum.GetValues<Gender>();



    public IReadOnlyList<StudentStatus> Statuses { get; } = Enum.GetValues<StudentStatus>();



    public bool IsNew => _studentId is null;



    public string StudentNumberLabel

    {

        get => _studentNumberLabel;

        private set => SetProperty(ref _studentNumberLabel, value);

    }



    public string FirstName

    {

        get => _firstName;

        set => SetProperty(ref _firstName, value);

    }



    public string LastName

    {

        get => _lastName;

        set => SetProperty(ref _lastName, value);

    }



    public Gender Gender

    {

        get => _gender;

        set => SetProperty(ref _gender, value);

    }



    public DateTime? DateOfBirth

    {

        get => _dateOfBirth;

        set => SetProperty(ref _dateOfBirth, value);

    }



    public string? Address

    {

        get => _address;

        set => SetProperty(ref _address, value);

    }



    public string? PhoneNumber

    {

        get => _phoneNumber;

        set => SetProperty(ref _phoneNumber, value);

    }



    public string? Email

    {

        get => _email;

        set => SetProperty(ref _email, value);

    }



    public AcademicLevelOption? SelectedLevel

    {

        get => _selectedLevel;

        set

        {

            if (SetProperty(ref _selectedLevel, value) && _lookupsReady)

            {

                _ = ReloadGroupsAsync();

            }

        }

    }



    public StudentGroupOption? SelectedGroup

    {

        get => _selectedGroup;

        set => SetProperty(ref _selectedGroup, value);

    }



    public SchoolYearOption? SelectedSchoolYear

    {

        get => _selectedSchoolYear;

        set => SetProperty(ref _selectedSchoolYear, value);

    }



    public DateTime? EnrollmentDate

    {

        get => _enrollmentDate;

        set => SetProperty(ref _enrollmentDate, value);

    }



    public StudentStatus Status

    {

        get => _status;

        set => SetProperty(ref _status, value);

    }



    public void InitializeForCreate()

    {

        _studentId = null;

        Title = "New student";

        ConfirmButtonText = "Create the student";

    }



    public void InitializeForEdit(int studentId)

    {

        _studentId = studentId;

        Title = "Edit a student";

        ConfirmButtonText = "Save";

    }



    public override async Task LoadAsync()

    {

        await RunGuardedAsync(async () =>

        {

            await LoadLookupsAsync();



            if (_studentId is null)

            {

                _selectedSchoolYear = SchoolYears.FirstOrDefault(year => year.IsCurrent) ?? SchoolYears.FirstOrDefault();

                OnPropertyChanged(nameof(SelectedSchoolYear));

                _selectedLevel = Levels.FirstOrDefault();

                OnPropertyChanged(nameof(SelectedLevel));

                await ReloadGroupsCoreAsync();

                await PreviewStudentNumberCoreAsync();

                _lookupsReady = true;

                return;

            }



            var detail = await _scopedExecutor.RunAsync(provider =>

                provider.GetRequiredService<IStudentService>().GetDetailAsync(_studentId.Value));



            StudentNumberLabel = detail.StudentNumber;

            FirstName = detail.FirstName;

            LastName = detail.LastName;

            Gender = detail.Gender;

            DateOfBirth = detail.DateOfBirth;

            Address = detail.Address;

            PhoneNumber = detail.PhoneNumber;

            Email = detail.Email;

            EnrollmentDate = detail.EnrollmentDate;

            Status = detail.Status;



            _selectedLevel = Levels.FirstOrDefault(option => option.Id == detail.AcademicLevelId);

            OnPropertyChanged(nameof(SelectedLevel));

            await ReloadGroupsCoreAsync();

            SelectedGroup = Groups.FirstOrDefault(option => option.Id == detail.StudentGroupId);

            _selectedSchoolYear = SchoolYears.FirstOrDefault(option => option.Id == detail.SchoolYearId);

            OnPropertyChanged(nameof(SelectedSchoolYear));

            _lookupsReady = true;

        });

    }



    protected override async Task<bool> SaveAsync()

    {

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))

        {

            ErrorMessage = "The first name and the last name are required.";

            return false;

        }



        if (DateOfBirth is null || EnrollmentDate is null)

        {

            ErrorMessage = "The date of birth and the enrollment date are required.";

            return false;

        }



        if (SelectedLevel is null)

        {

            ErrorMessage = "Select an academic level.";

            return false;

        }



        if (SelectedGroup is null)

        {

            ErrorMessage = "Select a student group.";

            return false;

        }



        await _scopedExecutor.RunAsync(async provider =>

        {

            var service = provider.GetRequiredService<IStudentService>();



            if (_studentId is null)

            {

                var id = await service.CreateAsync(new CreateStudentRequest(

                    FirstName,

                    LastName,

                    Gender,

                    DateOfBirth.Value,

                    Address,

                    PhoneNumber,

                    Email,

                    SelectedLevel.Id,

                    SelectedGroup.Id,

                    SelectedSchoolYear?.Id,

                    EnrollmentDate.Value,

                    Status));



                var created = await service.GetDetailAsync(id);

                StudentNumberLabel = created.StudentNumber;

                return;

            }



            await service.UpdateAsync(new UpdateStudentRequest(

                _studentId.Value,

                FirstName,

                LastName,

                Gender,

                DateOfBirth.Value,

                Address,

                PhoneNumber,

                Email,

                SelectedLevel.Id,

                SelectedGroup.Id,

                SelectedSchoolYear?.Id,

                EnrollmentDate.Value,

                Status));

        });



        return true;

    }



    private async Task LoadLookupsAsync()

    {

        var (levels, years) = await _scopedExecutor.RunAsync(async provider =>

        {

            var levelOptions = await provider.GetRequiredService<IAcademicLevelService>().ListOptionsAsync();

            var yearOptions = await provider.GetRequiredService<ISchoolYearService>().ListOptionsAsync();

            return (levelOptions, yearOptions);

        });



        Levels.Clear();

        foreach (var option in levels)

        {

            Levels.Add(option);

        }



        SchoolYears.Clear();

        foreach (var option in years)

        {

            SchoolYears.Add(option);

        }

    }



    private Task ReloadGroupsAsync() => RunGuardedAsync(ReloadGroupsCoreAsync);



    private async Task ReloadGroupsCoreAsync()

    {

        var groups = await _scopedExecutor.RunAsync(provider =>

            provider.GetRequiredService<IStudentGroupService>()

                .ListOptionsAsync(SelectedLevel?.Id, onlyActive: true));



        Groups.Clear();

        foreach (var option in groups)

        {

            Groups.Add(option);

        }



        if (SelectedGroup is not null && Groups.All(option => option.Id != SelectedGroup.Id))

        {

            SelectedGroup = Groups.FirstOrDefault();

        }

        else

        {

            SelectedGroup ??= Groups.FirstOrDefault();

        }

    }



    private async Task PreviewStudentNumberCoreAsync()

    {

        StudentNumberLabel = await _scopedExecutor.RunAsync(provider =>

            provider.GetRequiredService<IStudentService>().PeekNextStudentNumberAsync());

    }

}

