using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.AcademicLevels;
using SchoolManagement.Application.DTOs.Schedules;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.DTOs.Students;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.People;

/// <summary>
/// Independent student groups under each academic level, with the students of the
/// highlighted group shown beside the list.
/// </summary>
public class SchoolClassListViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private string? _searchTerm;
    private FilterOption<int>? _selectedLevel;
    private FilterOption<bool>? _selectedActivity;
    private StudentGroupListItem? _selectedClass;
    private StudentListItem? _selectedStudent;
    private ClassScheduleDto? _selectedSchedule;
    private bool _isInitialized;

    public SchoolClassListViewModel(
        ILogger<SchoolClassListViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        INavigationService navigationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        ActivityOptions = new List<FilterOption<bool>>
        {
            new(null, "Active and inactive"),
            new(true, "Active only"),
            new(false, "Inactive only")
        };
        _selectedActivity = ActivityOptions[1];

        SearchCommand = new AsyncRelayCommand(LoadClassesAsync);
        CreateCommand = new AsyncRelayCommand(CreateAsync, () => CanManageClasses);
        EditCommand = new AsyncRelayCommand(EditAsync, () => CanManageClasses && SelectedClass is not null);
        ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync, () => CanManageClasses && SelectedClass is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => CanManageClasses && SelectedClass is not null);
        OpenStudentCommand = new AsyncRelayCommand(OpenStudentAsync, () => SelectedStudent is not null);
        AddScheduleCommand = new AsyncRelayCommand(AddScheduleAsync, () => CanManageSchedules && SelectedClass is not null);
        EditScheduleCommand = new AsyncRelayCommand(EditScheduleAsync, () => CanManageSchedules && SelectedSchedule is not null);
        DeleteScheduleCommand = new AsyncRelayCommand(DeleteScheduleAsync, () => CanManageSchedules && SelectedSchedule is not null);
    }

    public bool CanManageClasses => _currentUser.HasPermission(Permission.ManageStudentGroups);

    public bool CanManageSchedules => _currentUser.HasPermission(Permission.ManageSchedules);

    public ObservableCollection<StudentGroupListItem> Classes { get; } = new();

    public ObservableCollection<StudentListItem> Students { get; } = new();

    public ObservableCollection<ClassScheduleDto> Schedules { get; } = new();

    public ObservableCollection<FilterOption<int>> LevelOptions { get; } = new();

    public IReadOnlyList<FilterOption<bool>> ActivityOptions { get; }

    public ICommand SearchCommand { get; }

    public ICommand CreateCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand ToggleActiveCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand OpenStudentCommand { get; }

    public ICommand AddScheduleCommand { get; }

    public ICommand EditScheduleCommand { get; }

    public ICommand DeleteScheduleCommand { get; }

    public string? SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public string ToggleActiveLabel => SelectedClass?.IsActive == true ? "Deactivate" : "Activate";

    public FilterOption<int>? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value) && _isInitialized)
            {
                _ = LoadClassesAsync();
            }
        }
    }

    public FilterOption<bool>? SelectedActivity
    {
        get => _selectedActivity;
        set
        {
            if (SetProperty(ref _selectedActivity, value) && _isInitialized)
            {
                _ = LoadClassesAsync();
            }
        }
    }

    public StudentGroupListItem? SelectedClass
    {
        get => _selectedClass;
        set
        {
            if (!SetProperty(ref _selectedClass, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ToggleActiveLabel));
            (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ToggleActiveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (AddScheduleCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

            _ = LoadStudentsAsync();
            _ = LoadSchedulesAsync();
        }
    }

    public StudentListItem? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (SetProperty(ref _selectedStudent, value))
            {
                (OpenStudentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ClassScheduleDto? SelectedSchedule
    {
        get => _selectedSchedule;
        set
        {
            if (SetProperty(ref _selectedSchedule, value))
            {
                (EditScheduleCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (DeleteScheduleCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public override async Task LoadAsync()
    {
        await RunGuardedAsync(async () =>
        {
            var levels = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAcademicLevelService>().ListOptionsAsync(onlyActive: false));

            LevelOptions.Clear();
            foreach (var option in FilterOption.ForItems("All levels", levels, level => level.Id, level => level.DisplayName))
            {
                LevelOptions.Add(option);
            }

            _selectedLevel = LevelOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedLevel));
        });

        await LoadClassesAsync();
        _isInitialized = true;
    }

    private Task LoadClassesAsync() =>
        RunGuardedAsync(async () =>
        {
            var groups = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IStudentGroupService>().ListAsync(
                    new StudentGroupFilter(SearchTerm, SelectedLevel?.Value, SelectedActivity?.Value)));

            var previousId = SelectedClass?.Id;

            Classes.Clear();
            foreach (var item in groups)
            {
                Classes.Add(item);
            }

            SelectedClass = Classes.FirstOrDefault(item => item.Id == previousId) ?? Classes.FirstOrDefault();
        });

    private Task LoadStudentsAsync()
    {
        if (SelectedClass is null)
        {
            Students.Clear();
            return Task.CompletedTask;
        }

        var groupId = SelectedClass.Id;

        return RunGuardedAsync(async () =>
        {
            var students = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IStudentGroupService>().ListStudentsAsync(groupId));

            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }
        });
    }

    private Task LoadSchedulesAsync()
    {
        if (SelectedClass is null)
        {
            Schedules.Clear();
            return Task.CompletedTask;
        }

        var groupId = SelectedClass.Id;

        return RunGuardedAsync(async () =>
        {
            var schedules = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IClassScheduleService>().ListByGroupAsync(groupId));

            Schedules.Clear();
            foreach (var schedule in schedules)
            {
                Schedules.Add(schedule);
            }
        });
    }

    private async Task CreateAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<SchoolClassEditViewModel>();
        viewModel.InitializeForCreate();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadClassesAsync();
            StatusMessage = "The student group has been created.";
        }
    }

    private async Task EditAsync()
    {
        if (SelectedClass is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<SchoolClassEditViewModel>();
        viewModel.InitializeForEdit(SelectedClass);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadClassesAsync();
            StatusMessage = "The student group has been updated.";
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedClass is null)
        {
            return;
        }

        var group = SelectedClass;
        var activate = !group.IsActive;

        var applied = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IStudentGroupService>().SetActiveAsync(group.Id, activate)));

        if (applied)
        {
            await LoadClassesAsync();
            StatusMessage = activate
                ? $"Group {group.Name} is active again."
                : $"Group {group.Name} has been deactivated.";
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedClass is null)
        {
            return;
        }

        var group = SelectedClass;

        if (!_dialogService.ConfirmCritical(
                $"Archive group {group.Name}?\n\nIt must not contain any student.",
                "Archive a group"))
        {
            return;
        }

        var deleted = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IStudentGroupService>().DeleteAsync(group.Id)));

        if (deleted)
        {
            await LoadClassesAsync();
            StatusMessage = $"Group {group.Name} has been archived.";
        }
    }

    private Task OpenStudentAsync()
    {
        if (SelectedStudent is null)
        {
            return Task.CompletedTask;
        }

        var studentId = SelectedStudent.Id;

        return _navigationService.NavigateToAsync<StudentDetailViewModel>(
            viewModel => viewModel.Initialize(studentId));
    }

    private async Task AddScheduleAsync()
    {
        if (SelectedClass is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<ClassScheduleEditViewModel>();
        viewModel.InitializeForCreate(SelectedClass.Id, SelectedClass.Name);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadSchedulesAsync();
            await LoadClassesAsync();
            StatusMessage = "The class session has been added.";
        }
    }

    private async Task EditScheduleAsync()
    {
        if (SelectedSchedule is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<ClassScheduleEditViewModel>();
        viewModel.InitializeForEdit(SelectedSchedule);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadSchedulesAsync();
            StatusMessage = "The class session has been updated.";
        }
    }

    private async Task DeleteScheduleAsync()
    {
        if (SelectedSchedule is null || SelectedClass is null)
        {
            return;
        }

        if (!_dialogService.Confirm(
                $"Remove the {SelectedSchedule.SessionLabel} session from {SelectedClass.Name}?",
                "Remove a session"))
        {
            return;
        }

        var scheduleId = SelectedSchedule.Id;
        var removed = await RunGuardedAsync(() => _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IClassScheduleService>().DeleteAsync(scheduleId)));

        if (removed)
        {
            await LoadSchedulesAsync();
            await LoadClassesAsync();
            StatusMessage = "The class session has been removed.";
        }
    }
}
