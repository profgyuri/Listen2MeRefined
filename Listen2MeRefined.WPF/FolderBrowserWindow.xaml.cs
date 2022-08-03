namespace Listen2MeRefined.WPF;
using System.Windows;

/// <summary>
/// Interaction logic for FolderBrowserWindow.xaml
/// </summary>
public partial class FolderBrowserWindow : Window
{
    public FolderBrowserWindow(FolderBrowserViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
