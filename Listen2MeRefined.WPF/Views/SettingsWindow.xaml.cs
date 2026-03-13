using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Application.ViewModels.Windows;
using Listen2MeRefined.WPF.Utils.Navigation;

namespace Listen2MeRefined.WPF.Views;

using System.Windows;
using System.Windows.Input;

/// <summary>
///     Interaction logic for SettingsWindow.xaml
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly IWindowManager _windowManager;
    
    private bool _isLoadedOnce;

    public ICommand HideWindowCommand { get; }

    public SettingsWindow(SettingsWindowViewModel viewModel, IWindowManager windowManager)
    {
        _windowManager = windowManager;
        HideWindowCommand = new RelayCommand(_ => Hide());
        InitializeComponent();

        DataContext = viewModel;

        Loaded += SettingsWindow_Loaded;
        IsVisibleChanged += SettingsWindow_IsVisibleChanged;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync().ConfigureAwait(false);
            _isLoadedOnce = true;
        }
    }

    private void SettingsWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!_isLoadedOnce || e.NewValue is not true)
        {
            return;
        }

        if (DataContext is SettingsWindowViewModel viewModel)
        {
            viewModel.RefreshLibraryTabData();
        }
    }

    private async void OpenFolderBrowser_Click(
        object sender,
        RoutedEventArgs e)
    {
        //await _windowManager.ShowWindowAsync<FolderBrowserWindow, FolderBrowserShellViewModel>("folderBrowser", Left + Width / 2, Top + Height / 2);
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Hide();
    }
}
