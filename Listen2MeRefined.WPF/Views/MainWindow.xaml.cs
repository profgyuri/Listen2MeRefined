using System.Windows;
using System.Windows.Input;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Windows;
using Listen2MeRefined.Infrastructure.Utils;

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
        MainWindowViewModel viewModel,
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
        DataContext = viewModel;
    }

    private void CloseWindow()
    {
        _globalHook.Unregister();
        System.Windows.Application.Current.Shutdown();
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
