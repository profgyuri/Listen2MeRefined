using System.Windows;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views;

namespace Listen2MeRefined.WPF.Utils.Navigation;

public interface IWindowManager
{
    Task<bool?> ShowWindowAsync<TWindow, TShellViewModel>(
        string initialRoute,
        double left,
        double top,
        bool isModal = true,
        CancellationToken ct = default)
        where TWindow : Window
        where TShellViewModel : ShellViewModelBase;

    CornerWindow ShowCornerWindow(int x, int y, int triggerAreaSize = 10);
    void CloseCornerWindow(CornerWindow? window);
}