using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Listen2MeRefined.Application.ViewModels.Controls;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class PlaylistPaneView : UserControl
{
    public PlaylistPaneView()
    {
        InitializeComponent();
    }

    private void OpenPlaylistPickerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
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
                Style = (Style?)FindResource("PlaylistPaneContextMenuItemStyle")
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

        var addToNewPlaylist = new MenuItem
        {
            Header = "Create New Playlist...",
            StaysOpenOnClick = true,
            Style = (Style?)FindResource("PlaylistPaneContextMenuItemStyle")
        };
        addToNewPlaylist.Click += (_, _) =>
            AddInlineNewPlaylistDraft(menu, addToNewPlaylist, viewModel.AddToNewPlaylistFromContextAsync);

        menu.Items.Add(addToNewPlaylist);
    }

    private void AddInlineNewPlaylistDraft(
        ContextMenu menu,
        MenuItem addToNewPlaylistMenuItem,
        Func<string, Task> createPlaylistAsync)
    {
        if (addToNewPlaylistMenuItem.Tag is MenuItem existingDraft &&
            existingDraft.Header is TextBox existingTextBox)
        {
            existingTextBox.Focus();
            existingTextBox.SelectAll();
            return;
        }

        var textBox = CreateInlinePlaylistNameEditor();
        var draftMenuItem = new MenuItem
        {
            Header = textBox,
            StaysOpenOnClick = true,
            Style = (Style?)FindResource("PlaylistPaneContextMenuItemStyle")
        };

        addToNewPlaylistMenuItem.Tag = draftMenuItem;
        addToNewPlaylistMenuItem.IsEnabled = false;

        var insertIndex = menu.Items.IndexOf(addToNewPlaylistMenuItem);
        if (insertIndex < 0)
        {
            menu.Items.Add(draftMenuItem);
        }
        else
        {
            menu.Items.Insert(insertIndex, draftMenuItem);
        }

        textBox.Loaded += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        textBox.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Escape)
            {
                menu.Items.Remove(draftMenuItem);
                addToNewPlaylistMenuItem.Tag = null;
                addToNewPlaylistMenuItem.IsEnabled = true;
                args.Handled = true;
                return;
            }

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

            textBox.IsEnabled = false;
            try
            {
                await createPlaylistAsync(newName);
                draftMenuItem.Header = newName;
                draftMenuItem.Focusable = false;
                addToNewPlaylistMenuItem.Tag = null;
                addToNewPlaylistMenuItem.IsEnabled = true;
            }
            catch
            {
                textBox.IsEnabled = true;
                textBox.Focus();
                textBox.SelectAll();
            }

            args.Handled = true;
        };
    }

    private TextBox CreateInlinePlaylistNameEditor()
    {
        return new TextBox
        {
            MinWidth = 180,
            Margin = new Thickness(0),
            Padding = new Thickness(4, 2, 4, 2),
            Background = (Brush?)FindResource("PrimaryBrush"),
            Foreground = (Brush?)FindResource("SecondaryBrush"),
            BorderBrush = (Brush?)FindResource("TertiaryBrush"),
            BorderThickness = new Thickness(1),
            ToolTip = "Type a name, Enter to create, Esc to cancel"
        };
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
