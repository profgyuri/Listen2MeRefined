namespace Listen2MeRefined.WPF;
using System.Windows;

/// <summary>
///     Interaction logic for FolderBrowserWindow.xaml
/// </summary>
public sealed partial class FolderBrowserWindow : Window
{
    private readonly FolderBrowserViewModel _viewModel;

    public FolderBrowserWindow(FolderBrowserViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += (_, __) => FilterTextBox.Focus();
    }

    private async void SelectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (await _viewModel.TryHandleSelectedPathAsync())
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

}
