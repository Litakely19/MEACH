using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs.Students;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Shared;

/// <summary>Student as shown in a picker list.</summary>
public record PickedStudent(
    int Id,
    string StudentNumber,
    string FullName,
    string? ClassName,
    decimal OutstandingBalance)
{
    public string DisplayName => $"{FullName} ({StudentNumber})";
}

/// <summary>
/// Search box that resolves a student, reused by the invoice and payment dialogs.
/// It stays a plain observable object so a dialog can embed it without pulling a
/// second view model out of the container.
/// </summary>
public class StudentPicker : ObservableObject
{
    private readonly IScopedExecutor _scopedExecutor;

    private string? _searchTerm;
    private PickedStudent? _selectedStudent;
    private bool _isSearching;

    public StudentPicker(IScopedExecutor scopedExecutor)
    {
        _scopedExecutor = scopedExecutor;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
    }

    public event EventHandler? SelectedStudentChanged;

    public ObservableCollection<PickedStudent> Results { get; } = new();

    public ICommand SearchCommand { get; }

    public string? SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set => SetProperty(ref _isSearching, value);
    }

    public PickedStudent? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (SetProperty(ref _selectedStudent, value))
            {
                SelectedStudentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Preselects a student, for a payment opened from a student file or a fee line.</summary>
    public async Task SelectAsync(int studentId)
    {
        var detail = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<IStudentService>().GetDetailAsync(studentId));

        var student = new PickedStudent(
            detail.Id,
            detail.StudentNumber,
            detail.FullName,
            detail.StudentGroupName,
            detail.OutstandingBalance);

        Results.Clear();
        Results.Add(student);
        SearchTerm = student.FullName;
        SelectedStudent = student;
    }

    private async Task SearchAsync()
    {
        IsSearching = true;

        try
        {
            var page = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IStudentService>().ListAsync(
                    new StudentFilter(SearchTerm, Page: 1, PageSize: 25)));

            Results.Clear();
            foreach (var student in page.Items)
            {
                Results.Add(new PickedStudent(
                    student.Id,
                    student.StudentNumber,
                    student.FullName,
                    student.StudentGroupName,
                    student.OutstandingBalance));
            }

            SelectedStudent = Results.Count == 1 ? Results[0] : null;
        }
        finally
        {
            IsSearching = false;
        }
    }
}
