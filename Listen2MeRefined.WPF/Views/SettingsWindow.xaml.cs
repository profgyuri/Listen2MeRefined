namespace Listen2MeRefined.WPF.Views;
using System.Windows;

/// <summary>
///     Interaction logic for SettingsWindow.xaml
/// </summary>
public sealed partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += SettingsWindow_Loaded;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync().ConfigureAwait(false);
        }
    }

    private async void OpenFolderBrowser_Click(
        object sender,
        RoutedEventArgs e)
    {
        await WindowManager.ShowWindowAsync<FolderBrowserWindow>(Left + Width / 2, Top + Height / 2);
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Hide();
    }
}