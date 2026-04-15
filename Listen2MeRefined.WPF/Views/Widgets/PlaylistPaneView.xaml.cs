using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.ViewModels.Widgets;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class PlaylistPaneView : UserControl
{
    // Watchdog that hides the external drop overlay when DragOver events stop
    // firing. WPF's PreviewDragLeave is unreliable when a drag is cancelled
    // outside the window or over an invalid target, so we poll instead: every
    // PreviewDragOver resets the timer, and if no event arrives within the
    // interval we assume the drag is over.
    private readonly DispatcherTimer _externalDropWatchdog;

    public PlaylistPaneView()
    {
        InitializeComponent();

        _externalDropWatchdog = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _externalDropWatchdog.Tick += (_, _) => HideExternalDropOverlay();

        WeakReferenceMessenger.Default.Register<ScrollToPlaylistIndexRequestedMessage>(this, (_, msg) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (msg.Value >= 0 && msg.Value < PlaylistListView.Items.Count)
                {
                    PlaylistListView.ScrollIntoView(PlaylistListView.Items[msg.Value]);
                }
            });
        });

        Unloaded += (_, _) => WeakReferenceMessenger.Default.Unregister<ScrollToPlaylistIndexRequestedMessage>(this);
    }

    private void PlaylistListView_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true))
        {
            // Let existing in-app drag/drop handlers (Gong) process non-file drags.
            return;
        }

        e.Effects = DragDropEffects.Copy;

        // Show the drop zone overlay and suppress the empty-state overlay so the
        // visual is consistent whether or not the playlist already has songs.
        ExternalDropOverlay.Visibility = Visibility.Visible;
        EmptyStateOverlay.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);

        // Reset the watchdog: as long as DragOver keeps firing the overlay stays.
        _externalDropWatchdog.Stop();
        _externalDropWatchdog.Start();

        e.Handled = true;
    }

    private void PlaylistListView_OnPreviewDragLeave(object sender, DragEventArgs e)
    {
        // The watchdog handles hiding; nothing to do here. Kept wired so the
        // event is explicitly observed without racing PreviewDragOver when
        // moving between child elements.
    }

    private async void PlaylistListView_OnPreviewDrop(object sender, DragEventArgs e)
    {
        HideExternalDropOverlay();

        if (DataContext is not PlaylistPaneViewModel vm ||
            !e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true) ||
            e.Data.GetData(DataFormats.FileDrop, autoConvert: true) is not string[] droppedFiles)
        {
            return;
        }

        var listView = (ListView)sender;
        var insertIndex = ResolveDropIndex(listView, e.GetPosition(listView));
        await vm.HandleExternalFileDropAsync(droppedFiles, insertIndex);
        e.Handled = true;
    }

    private void HideExternalDropOverlay()
    {
        _externalDropWatchdog.Stop();
        ExternalDropOverlay.Visibility = Visibility.Collapsed;
        // Restore the empty-state overlay's style-driven visibility.
        EmptyStateOverlay.ClearValue(VisibilityProperty);
    }

    private static int ResolveDropIndex(ListView listView, System.Windows.Point point)
    {
        var item = FindAncestor<ListViewItem>(listView.InputHitTest(point) as DependencyObject);
        if (item is null)
        {
            return listView.Items.Count;
        }

        var index = listView.ItemContainerGenerator.IndexFromContainer(item);
        var positionWithinItem = point.Y - item.TranslatePoint(new System.Windows.Point(0, 0), listView).Y;
        return positionWithinItem > item.ActualHeight / 2 ? index + 1 : index;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void ListViewOnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject;
        var listItem = FindVisualParent<ListViewItem>(source);
        if (listItem is null || listItem.IsSelected)
        {
            return;
        }

        listView.SelectedItems.Clear();
        listItem.IsSelected = true;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T parent)
            {
                return parent;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
