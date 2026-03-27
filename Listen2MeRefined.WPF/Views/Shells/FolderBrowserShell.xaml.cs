using System.Windows.Input;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using System.Windows;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class FolderBrowserShell : Window
{
    public FolderBrowserShell(FolderBrowserShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FolderBrowserShellViewModel shellViewModel
            || shellViewModel.CurrentViewModel is not FolderBrowserShellDefaultHomeViewModel homeViewModel)
        {
            return;
        }

        if (await homeViewModel.TryHandleSelectedPathAsync())
        {
            CloseWithResult(true);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseWithResult(false);
    }

    private void FolderBrowserShell_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        CloseWithResult(false);
        e.Handled = true;
    }

    private void CloseWithResult(bool result)
    {
        try
        {
            DialogResult = result;
        }
        catch (InvalidOperationException)
        {
            // Ignore when the window is shown modelessly.
        }

        Close();
    }
}
