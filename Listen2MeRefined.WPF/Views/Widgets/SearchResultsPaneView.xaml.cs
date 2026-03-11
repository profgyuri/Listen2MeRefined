using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Listen2MeRefined.Application.ViewModels.Controls;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class SearchResultsPaneView : UserControl
{
    public SearchResultsPaneView()
    {
        InitializeComponent();
    }

    private async void SongContextMenuOnOpened(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || DataContext is not SearchResultsPaneViewModel viewModel)
        {
            return;
        }

        await BuildSongContextMenuAsync(menu, viewModel);
    }

    private async Task BuildSongContextMenuAsync(ContextMenu menu, SearchResultsPaneViewModel viewModel)
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

        var addToNewPlaylist = new MenuItem
        {
            Header = "Add To New Playlist",
            StaysOpenOnClick = true,
            Style = (Style?)FindResource("PlaylistContextMenuItemStyle")
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
            Style = (Style?)FindResource("PlaylistContextMenuItemStyle")
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
