using System.Windows;
using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class PopupShell : Window
{
    public PopupShell(PopupShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}