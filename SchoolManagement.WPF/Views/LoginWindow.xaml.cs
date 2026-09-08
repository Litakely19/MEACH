using System.Windows;
using SchoolManagement.WPF.ViewModels;

namespace SchoolManagement.WPF.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.SignedIn += OnSignedIn;
        Closed += (_, _) => _viewModel.SignedIn -= OnSignedIn;

        Loaded += async (_, _) =>
        {
            UsernameBox.Focus();
            await _viewModel.LoadAsync();
        };
    }

    private void OnSignedIn(object? sender, EventArgs args)
    {
        DialogResult = true;
        Close();
    }
}
