using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.AcademicLevels;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class SchoolClassEditViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int? _groupId;
    private string _name = string.Empty;
    private string? _description;
    private bool _isActive = true;
    private AcademicLevelOption? _selectedLevel;

    public SchoolClassEditViewModel(ILogger<SchoolClassEditViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        DialogWidth = 520;
    }

    public ObservableCollection<AcademicLevelOption> Levels { get; } = new();

    public bool IsNew => _groupId is null;

    public bool CanChangeActivity => !IsNew;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public AcademicLevelOption? SelectedLevel
    {
        get => _selectedLevel;
        set => SetProperty(ref _selectedLevel, value);
    }

    public void InitializeForCreate()
    {
        _groupId = null;
        Title = "New student group";
        ConfirmButtonText = "Create the group";
    }

    public void InitializeForEdit(StudentGroupListItem group)
    {
        _groupId = group.Id;
        Title = $"Edit group {group.Name}";
        ConfirmButtonText = "Save";

        Name = group.Name;
        Description = group.Description;
        IsActive = group.IsActive;
        _pendingLevelId = group.AcademicLevelId;
    }

    private int? _pendingLevelId;

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var levels = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IAcademicLevelService>().ListOptionsAsync(onlyActive: false));

            Levels.Clear();
            foreach (var option in levels)
            {
                Levels.Add(option);
            }

            SelectedLevel = _pendingLevelId is int id
                ? levels.FirstOrDefault(level => level.Id == id) ?? levels.FirstOrDefault()
                : levels.FirstOrDefault(level => level.IsActive) ?? levels.FirstOrDefault();
        });

    protected override async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "The group name is required.";
            return false;
        }

        if (SelectedLevel is null)
        {
            ErrorMessage = "Select the academic level of the group.";
            return false;
        }

        await _scopedExecutor.RunAsync(async provider =>
        {
            var service = provider.GetRequiredService<IStudentGroupService>();

            if (_groupId is null)
            {
                await service.CreateAsync(new CreateStudentGroupRequest(SelectedLevel.Id, Name, Description));
                return;
            }

            await service.UpdateAsync(new UpdateStudentGroupRequest(
                _groupId.Value,
                SelectedLevel.Id,
                Name,
                Description,
                IsActive));
        });

        return true;
    }
}
