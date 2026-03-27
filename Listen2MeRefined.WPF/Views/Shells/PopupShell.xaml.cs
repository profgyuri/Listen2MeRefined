using System.Windows;
using System.Windows.Input;
using Listen2MeRefined.Application.ViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class PopupShell : Window
{
    public PopupShell(PopupShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void FooterBar_OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        CloseWithDecision(true);
    }

    private void FooterBar_OnSecondaryClick(object sender, RoutedEventArgs e)
    {
        CloseWithDecision(false);
    }

    private void PopupShell_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        CloseWithDecision(false);
        e.Handled = true;
    }

    private void CloseWithDecision(bool isConfirmed)
    {
        if (DataContext is PopupShellViewModel shellViewModel &&
            shellViewModel.CurrentViewModel is PopupViewModelBase popupViewModel)
        {
            if (isConfirmed)
            {
                popupViewModel.SendConfirmedMessage();
            }
            else
            {
                popupViewModel.SendCanceledMessage();
            }
        }

        try
        {
            DialogResult = isConfirmed;
        }
        catch (InvalidOperationException)
        {
            // Ignore when shown modelessly.
        }

        Close();
    }
}
