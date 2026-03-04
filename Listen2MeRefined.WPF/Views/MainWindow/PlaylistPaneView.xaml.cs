namespace Listen2MeRefined.WPF.Views.MainWindow;

using Listen2MeRefined.Infrastructure.ViewModels.MainWindow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

public partial class PlaylistPaneView : UserControl
{
    public PlaylistPaneView()
    {
        InitializeComponent();
    }

    private void PlaylistListView_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Let existing in-app drag/drop handlers (Gong) process non-file drags.
            return;
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void PlaylistListView_OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not PlaylistPaneViewModel vm ||
            !e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] droppedFiles)
        {
            return;
        }

        var listView = (ListView)sender;
        var insertIndex = ResolveDropIndex(listView, e.GetPosition(listView));
        await vm.HandleExternalFileDropAsync(droppedFiles, insertIndex);
        e.Handled = true;
    }

    private static int ResolveDropIndex(ListView listView, Point point)
    {
        var item = FindAncestor<ListViewItem>(listView.InputHitTest(point) as DependencyObject);
        if (item is null)
        {
            return listView.Items.Count;
        }

        var index = listView.ItemContainerGenerator.IndexFromContainer(item);
        var positionWithinItem = point.Y - item.TranslatePoint(new Point(0, 0), listView).Y;
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
}
