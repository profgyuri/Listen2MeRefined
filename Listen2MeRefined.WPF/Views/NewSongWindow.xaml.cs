using System.Windows;

namespace Listen2MeRefined.WPF.Views;

public sealed partial class NewSongWindow : Window
{
    public NewSongWindow(NewSongWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}