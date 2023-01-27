using Listen2MeRefined.WPF.Views.Pages;
using System.Windows;

namespace Listen2MeRefined.WPF.Views;

public sealed partial class NewSongWindow : Window
{
    public NewSongWindow(
        NewSongWindowViewModel viewModel, 
        CurrentlyPlayingPage currentlyPlayingPage)
    {
        InitializeComponent();

        DataContext = viewModel;
        ContentPage.Content = currentlyPlayingPage;
    }
}