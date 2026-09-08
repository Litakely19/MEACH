using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.AcademicLevels;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class AcademicLevelEditViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int? _levelId;
    private string _name = string.Empty;
    private string? _description;
    private bool _isActive = true;

    public AcademicLevelEditViewModel(
        ILogger<AcademicLevelEditViewModel> logger,
        IScopedExecutor scopedExecutor) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        DialogWidth = 480;
    }

    public bool IsNew => _levelId is null;

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

    public void InitializeForCreate()
    {
        _levelId = null;
        Title = "New academic level";
        ConfirmButtonText = "Create the level";
        Name = string.Empty;
        Description = null;
        IsActive = true;
    }

    public void InitializeForEdit(AcademicLevelListItem level)
    {
        _levelId = level.Id;
        Title = $"Edit {level.Name}";
        ConfirmButtonText = "Save";
        Name = level.Name;
        Description = level.Description;
        IsActive = level.IsActive;
    }

    protected override async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "The academic level name is required.";
            return false;
        }

        await _scopedExecutor.RunAsync(async provider =>
        {
            var service = provider.GetRequiredService<IAcademicLevelService>();

            if (_levelId is null)
            {
                await service.CreateAsync(new CreateAcademicLevelRequest(Name, Description));
                return;
            }

            await service.UpdateAsync(new UpdateAcademicLevelRequest(
                _levelId.Value,
                Name,
                Description,
                IsActive));
        });

        return true;
    }
}
