namespace Listen2MeRefined.WPF;

using Listen2MeRefined.WPF.Views;
using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
        System.Environment.Exit(0);
    }

    private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowState = 
            WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void SettingsWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowManager.ShowWindow<SettingsWindow>();
    }

    private void AdvancedSearchWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowManager.ShowWindow<AdvancedSearchWindow>();
    }
}