using System.Windows;
using System.Windows.Input;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Application.ViewModels.Windows;

namespace Listen2MeRefined.WPF;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IGlobalHook _globalHook;

    public ICommand CloseWindowCommand { get; }
    public ICommand MinimizeWindowCommand { get; }
    public ICommand ToggleWindowStateCommand { get; }

    public MainWindow(
        MainShellViewModelViewModel viewModelViewModel,
        IGlobalHook globalHook)
    {
        _globalHook = globalHook;

        // View-only commands for native window chrome operations.
        CloseWindowCommand = new RelayCommand(_ => CloseWindow());
        MinimizeWindowCommand = new RelayCommand(_ => WindowState = WindowState.Minimized);
        ToggleWindowStateCommand = new RelayCommand(_ =>
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized);

        InitializeComponent();
        DataContext = viewModelViewModel;
    }

    private void CloseWindow()
    {
        _globalHook.Unregister();
        System.Windows.Application.Current.Shutdown();
    }
}
