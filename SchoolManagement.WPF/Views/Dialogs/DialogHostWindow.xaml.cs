using System.Windows;
using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.Views.Dialogs;

public partial class DialogHostWindow : Window
{
    private readonly DialogViewModelBase _viewModel;

    public DialogHostWindow(DialogViewModelBase viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.RequestClose += OnRequestClose;
        Closed += (_, _) => _viewModel.RequestClose -= OnRequestClose;
    }

    private void OnRequestClose(object? sender, bool confirmed)
    {
        DialogResult = confirmed;
        Close();
    }
}
