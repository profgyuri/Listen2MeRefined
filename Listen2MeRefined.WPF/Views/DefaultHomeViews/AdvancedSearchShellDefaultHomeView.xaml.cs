using System.Windows;
using System.Windows.Controls;

namespace Listen2MeRefined.WPF.Views.DefaultHomeViews;

public partial class AdvancedSearchShellDefaultHomeView : UserControl
{
    public AdvancedSearchShellDefaultHomeView()
    {
        InitializeComponent();
    }

    private void AddCriteriaButton_Click(object sender, RoutedEventArgs e)
    {
        GeneralInput.Focus();
        GeneralInput.SelectAll();
    }

    private void EditCriteriaButton_Click(object sender, RoutedEventArgs e)
    {
        GeneralInput.Focus();
        GeneralInput.SelectAll();
    }
}