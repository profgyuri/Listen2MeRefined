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

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as AdvancedSearchViewModel;

        vm!.Search();

        Close();
    }
}