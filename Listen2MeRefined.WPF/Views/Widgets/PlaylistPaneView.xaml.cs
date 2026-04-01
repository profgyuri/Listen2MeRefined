using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Listen2MeRefined.Application.ViewModels.Widgets;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class PlaylistPaneView : UserControl
{
    public PlaylistPaneView()
    {
        InitializeComponent();
    }

    private void PlaylistListView_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true))
        {
            // Let existing in-app drag/drop handlers (Gong) process non-file drags.
            return;
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void PlaylistListView_OnPreviewDrop(object sender, DragEventArgs e)
    {
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
