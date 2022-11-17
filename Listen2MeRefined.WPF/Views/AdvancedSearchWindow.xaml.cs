using System.Windows;

namespace Listen2MeRefined.WPF.Views;

/// <summary>
///     Interaction logic for AdvancedSearchWindow.xaml
/// </summary>
public sealed partial class AdvancedSearchWindow : Window
{
    public AdvancedSearchWindow(AdvancedSearchViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}