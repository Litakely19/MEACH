using System.Windows;
using SchoolManagement.WPF.ViewModels.Shell;

namespace SchoolManagement.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.LoadAsync();
    }

    public MainViewModel ViewModel { get; }
}
