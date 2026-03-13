using System.Windows;
using System.Windows.Input;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class AdvancedSearchShell : Window
{
    public AdvancedSearchShell()
    {
        InitializeComponent();
    }

    private void AdvancedSearchShell_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        
        Close();
        e.Handled = true;
    }
}