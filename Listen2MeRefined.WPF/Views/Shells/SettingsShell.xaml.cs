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
}