namespace Listen2MeRefined.WPF.Views;
using System.Windows;

public sealed partial class NewSongWindow : Window
{
    public NewSongWindow(
        NewSongWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}