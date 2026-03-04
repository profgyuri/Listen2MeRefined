namespace Listen2MeRefined.WPF.Views.MainWindow;

using Listen2MeRefined.Infrastructure.ViewModels.MainWindow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

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

    private void PlusButtonOnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }

    private async void SongContextMenuOnOpened(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || DataContext is not PlaylistPaneViewModel viewModel)
        {
            return;
        }

        await BuildSongContextMenuAsync(menu, viewModel);
    }

    private async Task BuildSongContextMenuAsync(ContextMenu menu, PlaylistPaneViewModel viewModel)
    {
        menu.Items.Clear();

        var playlistStates = await viewModel.GetSongContextMenuPlaylistsAsync();
        foreach (var state in playlistStates)
        {
            var menuItem = new MenuItem
            {
                Header = state.PlaylistName,
                IsCheckable = true,
                IsChecked = state.IsChecked,
                Style = (Style?)FindResource("PlaylistContextMenuItemStyle")
            };

            menuItem.Click += async (_, _) =>
            {
                if (!state.AllowRemove && !menuItem.IsChecked)
                {
                    menuItem.IsChecked = true;
                    return;
                }

                try
                {
                    await viewModel.TogglePlaylistMembershipAsync(state.PlaylistId, menuItem.IsChecked, state.AllowRemove);
                }
                catch
                {
                    menuItem.IsChecked = state.IsChecked;
                }
            };

            menu.Items.Add(menuItem);
        }

        if (playlistStates.Count > 0)
        {
            menu.Items.Add(new Separator());
        }

        var textBox = new TextBox
        {
            MinWidth = 180,
            Margin = new System.Windows.Thickness(0),
            Padding = new System.Windows.Thickness(4, 2, 4, 2),
            Background = (System.Windows.Media.Brush?)FindResource("PrimaryBrush"),
            Foreground = (System.Windows.Media.Brush?)FindResource("SecondaryBrush"),
            BorderBrush = (System.Windows.Media.Brush?)FindResource("TertiaryBrush"),
            BorderThickness = new System.Windows.Thickness(1),
            ToolTip = "Type a name and press Enter"
        };

        textBox.KeyDown += async (_, args) =>
        {
            if (args.Key != Key.Enter)
            {
                return;
            }

            var newName = textBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(newName))
            {
                args.Handled = true;
                return;
            }

            try
            {
                await viewModel.AddToNewPlaylistFromContextAsync(newName);
                menu.IsOpen = false;
            }
            catch
            {
                // Swallow: invalid names and duplicates are handled by view models/services.
            }
            args.Handled = true;
        };

        var textBoxHost = new MenuItem
        {
            Header = textBox,
            StaysOpenOnClick = true,
            Style = (Style?)FindResource("PlaylistContextMenuItemStyle")
        };

        var addToNewPlaylist = new MenuItem
        {
            Header = "Add To New Playlist",
            StaysOpenOnClick = true,
            Style = (Style?)FindResource("PlaylistContextMenuItemStyle")
        };
        addToNewPlaylist.Items.Add(textBoxHost);
        addToNewPlaylist.SubmenuOpened += (_, _) => textBox.Focus();

        menu.Items.Add(addToNewPlaylist);
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
