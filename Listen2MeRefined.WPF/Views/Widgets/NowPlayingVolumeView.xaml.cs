using System.Windows.Controls;
using System.Windows.Input;
using Listen2MeRefined.Application.ViewModels.Widgets;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class NowPlayingVolumeView : UserControl
{
    public NowPlayingVolumeView()
    {
        InitializeComponent();
    }

    private void Slider_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is NowPlayingVolumeViewModel vm)
        {
            vm.AdjustVolumeByDelta(e.Delta);
            e.Handled = true;
        }
    }
}
