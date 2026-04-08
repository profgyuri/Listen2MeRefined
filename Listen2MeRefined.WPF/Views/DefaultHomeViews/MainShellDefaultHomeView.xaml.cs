using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

namespace Listen2MeRefined.WPF.Views.DefaultHomeViews;

public partial class MainShellDefaultHomeView : UserControl
{
    public MainShellDefaultHomeView()
    {
        InitializeComponent();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainShellDefaultHomeViewModel vm)
        {
            return;
        }

        var focusedElement = Keyboard.FocusedElement as DependencyObject;
        var isTextBoxFocused = IsInsideControl<TextBox>(focusedElement);
        var isListFocused = IsInsideControl<ListView>(focusedElement) || IsInsideControl<ListBox>(focusedElement);

        switch (e.Key)
        {
            case Key.Space when !isTextBoxFocused && Keyboard.Modifiers == ModifierKeys.None:
                vm.PlaybackControlsViewModel.PlayPauseCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.M when !isTextBoxFocused && Keyboard.Modifiers == ModifierKeys.None:
                vm.NowPlayingVolumeViewModel.ToggleMuteCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up when !isTextBoxFocused && !isListFocused && Keyboard.Modifiers == ModifierKeys.None:
                vm.NowPlayingVolumeViewModel.VolumeUpCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down when !isTextBoxFocused && !isListFocused && Keyboard.Modifiers == ModifierKeys.None:
                vm.NowPlayingVolumeViewModel.VolumeDownCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private static bool IsInsideControl<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T)
            {
                return true;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return false;
    }
}
