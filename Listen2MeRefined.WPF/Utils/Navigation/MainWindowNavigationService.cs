using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views;

namespace Listen2MeRefined.WPF.Utils.Navigation;

public sealed class MainWindowNavigationService : IMainWindowNavigationService
{
    private readonly IWindowManager _windowManager;

    public MainWindowNavigationService(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public async Task OpenSettingsAsync()
    {
        var window = System.Windows.Application.Current.MainWindow;
        if (window is null)
        {
            return;
        }

        await _windowManager.ShowWindowAsync<SettingsWindow, SettingsShellViewModel>("settings", window.Left + window.Width / 2, window.Top + window.Height / 2);
    }

    public async Task OpenAdvancedSearchAsync()
    {
        var window = System.Windows.Application.Current.MainWindow;
        if (window is null)
        {
            return;
        }

        await _windowManager.ShowWindowAsync<AdvancedSearchWindow, AdvancedSearchShellViewModel>("advancedSearch", window.Left + window.Width / 2, window.Top + window.Height / 2);
    }
}
