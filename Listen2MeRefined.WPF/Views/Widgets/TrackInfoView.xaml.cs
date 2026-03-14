using System.Windows.Controls;
using Listen2MeRefined.Application.ViewModels.Widgets;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class TrackInfoView : UserControl
{
    public TrackInfoView(TrackInfoViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}
