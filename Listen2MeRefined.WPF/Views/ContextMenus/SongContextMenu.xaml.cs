using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Listen2MeRefined.Application.ViewModels.ContextMenus;

namespace Listen2MeRefined.WPF.Views.ContextMenus;

public partial class SongContextMenu : ContextMenu
{
    public SongContextMenu()
    {
        InitializeComponent();
        Opened += SongContextMenuOnOpened;
        Closed += SongContextMenuOnClosed;
    }

    private async void SongContextMenuOnOpened(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SongContextMenuViewModel viewModel)
        {
            Items.Clear();
            return;
        }

        await viewModel.HandleOpenedAsync();
        BuildMenu(viewModel);
    }

    private void SongContextMenuOnClosed(object sender, RoutedEventArgs e)
    {
        if (DataContext is SongContextMenuViewModel viewModel)
        {
            viewModel.HandleClosed();
        }
    }

    private void BuildMenu(SongContextMenuViewModel viewModel)
    {
        Items.Clear();
        Items.Add(BuildPlaylistMenu(viewModel));

        if (!viewModel.ShowPlaylistActions)
        {
            return;
        }

        Items.Add(new Separator());
        Items.Add(CreateActionMenuItem("Rescan", () => viewModel.RescanAsync()));
        Items.Add(CreateActionMenuItem("Play now", () => viewModel.PlayNowAsync()));
        Items.Add(CreateActionMenuItem("Play after current", () => viewModel.PlayAfterCurrentAsync()));

        if (viewModel.ShowRemoveFromPlaylistAction)
        {
            Items.Add(CreateActionMenuItem("Remove from playlist", () => viewModel.RemoveFromPlaylistAsync()));
        }
    }

    private MenuItem BuildPlaylistMenu(SongContextMenuViewModel viewModel)
    {
        var playlistMenuItem = CreateStyledMenuItem("Playlist");

        foreach (var state in viewModel.Playlists)
        {
            var membershipMenuItem = CreateStyledMenuItem(state.PlaylistName);
            membershipMenuItem.IsCheckable = true;
            membershipMenuItem.IsChecked = state.IsChecked;
            membershipMenuItem.Click += async (_, _) =>
            {
                var previousChecked = state.IsChecked;
                var shouldContain = membershipMenuItem.IsChecked;
                if (!state.AllowRemove && !shouldContain)
                {
                    membershipMenuItem.IsChecked = true;
                    return;
                }

                try
                {
                    await viewModel.TogglePlaylistMembershipAsync(state, shouldContain);
                    state.IsChecked = shouldContain;
                }
                catch
                {
                    membershipMenuItem.IsChecked = previousChecked;
                    state.IsChecked = previousChecked;
                }
            };

            playlistMenuItem.Items.Add(membershipMenuItem);
        }

        if (playlistMenuItem.Items.Count > 0)
        {
            playlistMenuItem.Items.Add(new Separator());
        }

        var addToNewPlaylistMenuItem = CreateStyledMenuItem("Add To New Playlist");
        addToNewPlaylistMenuItem.StaysOpenOnClick = true;
        addToNewPlaylistMenuItem.Click += (_, _) =>
            AddInlineNewPlaylistDraft(viewModel, addToNewPlaylistMenuItem, playlistMenuItem.Items);
        playlistMenuItem.Items.Add(addToNewPlaylistMenuItem);

        return playlistMenuItem;
    }

    private void AddInlineNewPlaylistDraft(
        SongContextMenuViewModel viewModel,
        MenuItem addToNewPlaylistMenuItem,
        ItemCollection parentItems)
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

        var insertIndex = parentItems.IndexOf(addToNewPlaylistMenuItem);
        if (insertIndex < 0)
        {
            parentItems.Add(draftMenuItem);
        }
        else
        {
            parentItems.Insert(insertIndex, draftMenuItem);
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
                parentItems.Remove(draftMenuItem);
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
                await viewModel.AddToNewPlaylistAsync(newName);
                BuildMenu(viewModel);
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

    private MenuItem CreateActionMenuItem(string header, Func<Task> action)
    {
        var menuItem = CreateStyledMenuItem(header);
        menuItem.Click += async (_, _) => await action();
        return menuItem;
    }

    private MenuItem CreateStyledMenuItem(string header)
    {
        return new MenuItem
        {
            Header = header,
            Style = (Style?)FindResource("PlaylistContextMenuItemStyle")
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
}
