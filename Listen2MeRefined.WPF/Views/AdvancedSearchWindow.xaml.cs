namespace Listen2MeRefined.WPF.Views;
using System.Windows;
using System.Windows.Input;

/// <summary>
///     Interaction logic for AdvancedSearchWindow.xaml
/// </summary>
public sealed partial class AdvancedSearchWindow : Window
{
    public AdvancedSearchWindow(AdvancedSearchViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void ToggleWindowStateButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
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

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
