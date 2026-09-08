using System.Windows;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.ViewModels.Dialogs;
using SchoolManagement.WPF.Views.Dialogs;

namespace SchoolManagement.WPF.Services;

public class DialogService : IDialogService
{
    public void ShowInformation(string message, string title = "Information") =>
        MessageBox.Show(GetOwner(), message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message, string title = "Error") =>
        MessageBox.Show(GetOwner(), message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string message, string title = "Confirmation") =>
        MessageBox.Show(GetOwner(), message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;

    public bool ConfirmCritical(string message, string title = "Please confirm") =>
        MessageBox.Show(
            GetOwner(),
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;

    public async Task<bool> ShowDialogAsync(DialogViewModelBase viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        // Loading before the window appears keeps pickers and lists populated from
        // the first frame instead of flickering into place.
        await viewModel.LoadAsync();

        var window = new DialogHostWindow(viewModel)
        {
            Owner = GetOwner()
        };

        return window.ShowDialog() == true;
    }

    public string? AskForText(string message, string title, string? initialValue = null)
    {
        var viewModel = new TextPromptViewModel(message, title, initialValue);
        var window = new DialogHostWindow(viewModel)
        {
            Owner = GetOwner()
        };

        return window.ShowDialog() == true ? viewModel.Value : null;
    }

    // Qualified because SchoolManagement.Application shadows the WPF Application type here.
    private static Window? GetOwner() =>
        System.Windows.Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive)
        ?? System.Windows.Application.Current?.MainWindow;
}
