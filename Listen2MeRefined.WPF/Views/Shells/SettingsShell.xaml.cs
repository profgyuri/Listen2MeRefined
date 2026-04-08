using System.Windows;
using System.Windows.Input;
using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class SettingsShell : Window
{
    public ICommand CloseWindowCommand { get; }
    
    public SettingsShell(SettingsShellViewModel viewModel)
    {
        CloseWindowCommand = new RelayCommand(_ => Close());
        
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SettingsShell_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not SettingsShellViewModel vm)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;

            case Key.Tab when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift):
                vm.NavigateToPreviousTabCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Tab when Keyboard.Modifiers == ModifierKeys.Control:
                vm.NavigateToNextTabCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
