using Listen2MeRefined.Application.ViewModels.Windows;

namespace Listen2MeRefined.WPF.Views;

using System.Windows;
using System.Windows.Input;

/// <summary>
///     Interaction logic for SettingsWindow.xaml
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private bool _isLoadedOnce;

    public ICommand HideWindowCommand { get; }

    public SettingsWindow(SettingsWindowViewModel viewModel)
    {
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
        await WindowManager.ShowWindowAsync<FolderBrowserWindow>(Left + Width / 2, Top + Height / 2);
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Hide();
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
