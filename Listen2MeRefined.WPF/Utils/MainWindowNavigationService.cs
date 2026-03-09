using Listen2MeRefined.WPF.Views;
using System.Windows;
using Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

namespace Listen2MeRefined.WPF.Utils;

public sealed class MainWindowNavigationService : IMainWindowNavigationService
{
    public async Task OpenSettingsAsync()
    {
        var window = System.Windows.Application.Current.MainWindow;
        if (window is null)
        {
            return;
        }

        await WindowManager.ShowWindowAsync<SettingsWindow>(window.Left + window.Width / 2, window.Top + window.Height / 2);
    }

    public async Task OpenAdvancedSearchAsync()
    {
        var window = System.Windows.Application.Current.MainWindow;
        if (window is null)
        {
            return;
        }

        await WindowManager.ShowWindowAsync<AdvancedSearchWindow>(window.Left + window.Width / 2, window.Top + window.Height / 2);
    }
}
