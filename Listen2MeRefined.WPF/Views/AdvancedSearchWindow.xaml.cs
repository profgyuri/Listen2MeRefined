namespace Listen2MeRefined.WPF.Views;
using System.Windows;

/// <summary>
/// Interaction logic for AdvancedSearchWindow.xaml
/// </summary>
public sealed partial class AdvancedSearchWindow : Window
{
    public AdvancedSearchWindow(AdvancedSearchViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
