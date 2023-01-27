using Listen2MeRefined.Infrastructure.Mvvm.Pages;
using System.Windows.Controls;

namespace Listen2MeRefined.WPF.Views.Pages
{
    /// <summary>
    /// Interaction logic for CurrentlyPlayingPage.xaml
    /// </summary>
    public partial class CurrentlyPlayingPage : Page
    {
        public CurrentlyPlayingPage(CurrentlyPlayingPageViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
