using System.Windows;
using System.Windows.Controls;

namespace SchoolManagement.WPF.Views.People;

public partial class StudentListView : UserControl
{
    public StudentListView()
    {
        InitializeComponent();
    }

    private void CreateBillButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }
}
