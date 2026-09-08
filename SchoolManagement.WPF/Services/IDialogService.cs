using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.Services;

public interface IDialogService
{
    void ShowInformation(string message, string title = "Information");

    void ShowError(string message, string title = "Error");

    bool Confirm(string message, string title = "Confirmation");

    /// <summary>Extra confirmation step for irreversible or financial operations.</summary>
    bool ConfirmCritical(string message, string title = "Please confirm");

    /// <summary>Shows a modal dialog and returns true when the user confirmed it.</summary>
    Task<bool> ShowDialogAsync(DialogViewModelBase viewModel);

    string? AskForText(string message, string title, string? initialValue = null);
}
