using Listen2MeRefined.Application.ViewModels.Windows;

namespace Listen2MeRefined.WPF.Views;
using System.Windows;

public sealed partial class CornerWindow : Window
{
    public CornerWindow(
        CornerWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}