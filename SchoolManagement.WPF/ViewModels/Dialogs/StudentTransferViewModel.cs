using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.DTOs.Students;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class StudentTransferViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int _studentId;
    private string _studentName = string.Empty;
    private string _currentClassName = "No group";
    private StudentGroupOption? _targetClass;
    private string? _reason;

    public StudentTransferViewModel(ILogger<StudentTransferViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        Title = "Transfer a student";
        ConfirmButtonText = "Transfer";
        DialogWidth = 520;
    }

    public ObservableCollection<StudentGroupOption> Classes { get; } = new();

    public string StudentName
    {
        get => _studentName;
        private set => SetProperty(ref _studentName, value);
    }

    public string CurrentClassName
    {
        get => _currentClassName;
        private set => SetProperty(ref _currentClassName, value);
    }

    public StudentGroupOption? TargetClass
    {
        get => _targetClass;
        set => SetProperty(ref _targetClass, value);
    }

    public string? Reason
    {
        get => _reason;
        set => SetProperty(ref _reason, value);
    }

    public void Initialize(int studentId, string studentName, string? currentClassName)
    {
        _studentId = studentId;
        StudentName = studentName;
        CurrentClassName = string.IsNullOrWhiteSpace(currentClassName) ? "No group" : currentClassName;
    }

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var groups = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IStudentGroupService>().ListOptionsAsync());

            Classes.Clear();
            foreach (var option in groups)
            {
                Classes.Add(option);
            }
        });

    protected override async Task<bool> SaveAsync()
    {
        if (TargetClass is null)
        {
            ErrorMessage = "Select the destination group.";
            return false;
        }

        await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IStudentService>().TransferAsync(
                new TransferStudentRequest(_studentId, TargetClass.Id, Reason)));

        return true;
    }
}
