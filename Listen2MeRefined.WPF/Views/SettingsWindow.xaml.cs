using System.Windows;

namespace Listen2MeRefined.WPF.Views;

/// <summary>
///     Interaction logic for SettingsWindow.xaml
/// </summary>
public sealed partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private void OpenFolderBrowser_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowManager.ShowWindow<FolderBrowserWindow>(Left + Width / 2, Top + Height / 2);
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}