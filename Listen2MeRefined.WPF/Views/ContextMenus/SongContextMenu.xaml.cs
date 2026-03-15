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

        foreach (var state in viewModel.Playlists)
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
                var previousChecked = state.IsChecked;
                var shouldContain = menuItem.IsChecked;
                if (!state.AllowRemove && !shouldContain)
                {
                    menuItem.IsChecked = true;
                    return;
                }

                try
                {
                    await viewModel.TogglePlaylistMembershipAsync(state, shouldContain);
                    state.IsChecked = shouldContain;
                }
                catch
                {
                    menuItem.IsChecked = previousChecked;
                    state.IsChecked = previousChecked;
                }
            };

            Items.Add(menuItem);
        }

        if (viewModel.Playlists.Count > 0)
        {
            Items.Add(new Separator());
        }

        var addToNewPlaylistMenuItem = new MenuItem
        {
            Header = "Add To New Playlist",
            StaysOpenOnClick = true,
            Style = (Style?)FindResource("PlaylistContextMenuItemStyle")
        };
        addToNewPlaylistMenuItem.Click += (_, _) =>
            AddInlineNewPlaylistDraft(viewModel, addToNewPlaylistMenuItem);

        Items.Add(addToNewPlaylistMenuItem);
    }

    private void AddInlineNewPlaylistDraft(
        SongContextMenuViewModel viewModel,
        MenuItem addToNewPlaylistMenuItem)
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

        var insertIndex = Items.IndexOf(addToNewPlaylistMenuItem);
        if (insertIndex < 0)
        {
            Items.Add(draftMenuItem);
        }
        else
        {
            Items.Insert(insertIndex, draftMenuItem);
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
                Items.Remove(draftMenuItem);
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
