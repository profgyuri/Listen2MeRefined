using System.Windows;
using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class CornerWindowShell : Window
{
    public CornerWindowShell(CornerWindowShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
