using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.AcademicLevels;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels.Dialogs;

namespace SchoolManagement.WPF.ViewModels.People;

public class AcademicLevelListViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;
    private AcademicLevelListItem? _selectedLevel;

    public AcademicLevelListViewModel(
        ILogger<AcademicLevelListViewModel> logger,
        IScopedExecutor scopedExecutor,
        IDialogService dialogService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _dialogService = dialogService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        CreateCommand = new AsyncRelayCommand(CreateAsync, () => CanManage);
        EditCommand = new AsyncRelayCommand(EditAsync, () => CanManage && SelectedLevel is not null);
        ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync, () => CanManage && SelectedLevel is not null);
    }

    public bool CanManage => _currentUser.HasPermission(Permission.ManageAcademicLevels);

    public ObservableCollection<AcademicLevelListItem> Levels { get; } = new();

    public ObservableCollection<StudentGroupListItem> Groups { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand CreateCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand ToggleActiveCommand { get; }

    public string ToggleActiveLabel => SelectedLevel?.IsActive == true ? "Deactivate" : "Activate";

    public AcademicLevelListItem? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
            {
                OnPropertyChanged(nameof(ToggleActiveLabel));
                (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ToggleActiveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                _ = LoadGroupsAsync();
            }
        }
    }

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var levels = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAcademicLevelService>().ListAsync());

            var previousId = SelectedLevel?.Id;
            Levels.Clear();
            foreach (var level in levels)
            {
                Levels.Add(level);
            }

            // Set the field directly so LoadGroupsAsync is not nested under IsBusy
            // (RunGuardedAsync refuses nested calls and would skip the first groups load).
            var selected = Levels.FirstOrDefault(level => level.Id == previousId) ?? Levels.FirstOrDefault();
            if (!Equals(_selectedLevel, selected))
            {
                _selectedLevel = selected;
                OnPropertyChanged(nameof(SelectedLevel));
                OnPropertyChanged(nameof(ToggleActiveLabel));
                (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ToggleActiveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }

            await LoadGroupsCoreAsync();
        });

    private Task LoadGroupsAsync()
    {
        if (SelectedLevel is null)
        {
            Groups.Clear();
            return Task.CompletedTask;
        }

        return RunGuardedAsync(LoadGroupsCoreAsync);
    }

    private async Task LoadGroupsCoreAsync()
    {
        if (SelectedLevel is null)
        {
            Groups.Clear();
            return;
        }

        var levelId = SelectedLevel.Id;
        var groups = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IAcademicLevelService>().ListGroupsAsync(levelId));

        Groups.Clear();
        foreach (var group in groups)
        {
            Groups.Add(group);
        }
    }

    private async Task CreateAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<AcademicLevelEditViewModel>();
        viewModel.InitializeForCreate();

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The academic level has been created.";
        }
    }

    private async Task EditAsync()
    {
        if (SelectedLevel is null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<AcademicLevelEditViewModel>();
        viewModel.InitializeForEdit(SelectedLevel);

        if (await _dialogService.ShowDialogAsync(viewModel))
        {
            await LoadAsync();
            StatusMessage = "The academic level has been updated.";
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedLevel is null)
        {
            return;
        }

        var level = SelectedLevel;
        await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IAcademicLevelService>().SetActiveAsync(level.Id, !level.IsActive));
        await LoadAsync();
    }
}
