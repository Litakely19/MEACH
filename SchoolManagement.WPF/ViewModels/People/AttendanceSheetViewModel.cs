using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.AcademicLevels;
using SchoolManagement.Application.DTOs.Attendance;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.People;

public class AttendanceSheetStudentRow : ObservableObject
{
    private AttendanceStatus _status;
    private string? _remarks;

    public AttendanceSheetStudentRow(AttendanceSheetStudent student)
    {
        StudentId = student.StudentId;
        StudentNumber = student.StudentNumber;
        StudentName = student.StudentName;
        _status = student.Status;
        _remarks = student.Remarks;
    }

    public int StudentId { get; }

    public string StudentNumber { get; }

    public string StudentName { get; }

    public AttendanceStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string? Remarks
    {
        get => _remarks;
        set => SetProperty(ref _remarks, value);
    }
}

public class AttendanceSheetViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly ICurrentUserService _currentUser;
    private AcademicLevelOption? _selectedLevel;
    private StudentGroupOption? _selectedGroup;
    private AttendanceSheetStudentRow? _selectedStudent;
    private DateTime? _date = DateTime.Today;
    private AttendanceSheet? _sheet;
    private bool _isInitialized;

    public AttendanceSheetViewModel(
        ILogger<AttendanceSheetViewModel> logger,
        IScopedExecutor scopedExecutor,
        ICurrentUserService currentUser) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _currentUser = currentUser;

        LoadSheetCommand = new AsyncRelayCommand(LoadSheetAsync, () => SelectedGroup is not null && Date is not null);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanManage && Students.Count > 0);
        MarkAllPresentCommand = new RelayCommand(() => SetAll(AttendanceStatus.Present), () => CanManage && Students.Count > 0);
        MarkSelectedAbsentCommand = new RelayCommand(
            () =>
            {
                if (SelectedStudent is not null)
                {
                    SelectedStudent.Status = AttendanceStatus.Absent;
                }
            },
            () => CanManage && SelectedStudent is not null);
    }

    public bool CanManage => _currentUser.HasPermission(Permission.ManageAttendance);

    public ObservableCollection<AcademicLevelOption> Levels { get; } = new();

    public ObservableCollection<StudentGroupOption> Groups { get; } = new();

    public ObservableCollection<AttendanceSheetStudentRow> Students { get; } = new();

    public IReadOnlyList<AttendanceStatus> Statuses { get; } = Enum.GetValues<AttendanceStatus>();

    public ICommand LoadSheetCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand MarkAllPresentCommand { get; }

    public ICommand MarkSelectedAbsentCommand { get; }

    public AcademicLevelOption? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value) && _isInitialized)
            {
                _ = ReloadGroupsAsync();
            }
        }
    }

    public StudentGroupOption? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                (LoadSheetCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public AttendanceSheetStudentRow? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (SetProperty(ref _selectedStudent, value))
            {
                (MarkSelectedAbsentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTime? Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public AttendanceSheet? Sheet
    {
        get => _sheet;
        private set
        {
            if (SetProperty(ref _sheet, value))
            {
                OnPropertyChanged(nameof(SessionLabel));
            }
        }
    }

    public string SessionLabel => Sheet?.Session is { } session
        ? $"{session.DayOfWeek}: {session.SessionLabel}"
        : "No class session is scheduled for this day.";

    public override async Task LoadAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var levels = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAcademicLevelService>().ListOptionsAsync());

            Levels.Clear();
            foreach (var level in levels)
            {
                Levels.Add(level);
            }

            _selectedLevel = Levels.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedLevel));
            await ReloadGroupsCoreAsync();
            _isInitialized = true;
        });
    }

    private Task ReloadGroupsAsync() => RunGuardedAsync(ReloadGroupsCoreAsync);

    private async Task ReloadGroupsCoreAsync()
    {
        var groups = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IStudentGroupService>().ListOptionsAsync(SelectedLevel?.Id));

        Groups.Clear();
        foreach (var group in groups)
        {
            Groups.Add(group);
        }

        SelectedGroup = Groups.FirstOrDefault();
    }

    private Task LoadSheetAsync() =>
        RunGuardedAsync(async () =>
        {
            if (SelectedGroup is null || Date is null)
            {
                return;
            }

            var sheet = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAttendanceService>()
                    .GetSheetAsync(SelectedGroup.Id, Date.Value));

            Sheet = sheet;
            Students.Clear();
            foreach (var student in sheet.Students)
            {
                Students.Add(new AttendanceSheetStudentRow(student));
            }

            (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (MarkAllPresentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (MarkSelectedAbsentCommand as RelayCommand)?.RaiseCanExecuteChanged();
        });

    private void SetAll(AttendanceStatus status)
    {
        foreach (var student in Students)
        {
            student.Status = status;
        }
    }

    private Task SaveAsync() =>
        RunGuardedAsync(async () =>
        {
            if (SelectedGroup is null || Date is null || Sheet?.Session is null)
            {
                ErrorMessage = "Select a group that has a class session on this day.";
                return;
            }

            await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAttendanceService>().SaveAsync(
                    new SaveAttendanceRequest(
                        SelectedGroup.Id,
                        Sheet.Session.ClassScheduleId,
                        Date.Value,
                        Students.Select(student => new AttendanceEntry(student.StudentId, student.Status, student.Remarks)).ToList())));

            StatusMessage = "Attendance has been saved.";
        });
}

public class AttendanceReportViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private AcademicLevelOption? _selectedLevel;
    private StudentGroupOption? _selectedGroup;
    private DateTime? _from = DateTime.Today.AddMonths(-1);
    private DateTime? _to = DateTime.Today;

    public AttendanceReportViewModel(
        ILogger<AttendanceReportViewModel> logger,
        IScopedExecutor scopedExecutor) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        RunCommand = new AsyncRelayCommand(LoadReportAsync);
    }

    public ObservableCollection<AcademicLevelOption> Levels { get; } = new();

    public ObservableCollection<StudentGroupOption> Groups { get; } = new();

    public ObservableCollection<AttendanceReportRow> Rows { get; } = new();

    public ICommand RunCommand { get; }

    public AcademicLevelOption? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
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

    public DateTime? From
    {
        get => _from;
        set => SetProperty(ref _from, value);
    }

    public DateTime? To
    {
        get => _to;
        set => SetProperty(ref _to, value);
    }

    public override async Task LoadAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var levels = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAcademicLevelService>().ListOptionsAsync(onlyActive: false));

            Levels.Clear();
            foreach (var level in levels)
            {
                Levels.Add(level);
            }
        });

        await ReloadGroupsAsync();
        await LoadReportAsync();
    }

    private Task ReloadGroupsAsync() =>
        RunGuardedAsync(async () =>
        {
            var groups = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IStudentGroupService>().ListOptionsAsync(SelectedLevel?.Id, onlyActive: false));

            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }

            SelectedGroup = Groups.FirstOrDefault();
        });

    private Task LoadReportAsync() =>
        RunGuardedAsync(async () =>
        {
            var rows = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAttendanceService>().GetReportAsync(
                    new AttendanceReportFilter(
                        AcademicLevelId: SelectedLevel?.Id,
                        StudentGroupId: SelectedGroup?.Id,
                        From: From,
                        To: To)));

            Rows.Clear();
            foreach (var row in rows)
            {
                Rows.Add(row);
            }
        });
}
