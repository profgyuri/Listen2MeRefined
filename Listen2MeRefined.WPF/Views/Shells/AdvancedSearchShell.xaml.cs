using System.Windows;
using System.Windows.Input;
using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class AdvancedSearchShell : Window
{
    public AdvancedSearchShell(AdvancedSearchShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void AdvancedSearchShell_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;

        Close();
        e.Handled = true;
    }
}
