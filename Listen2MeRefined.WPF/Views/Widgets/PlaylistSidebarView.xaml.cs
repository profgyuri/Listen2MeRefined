using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Listen2MeRefined.Application.ViewModels.Widgets;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class PlaylistSidebarView : UserControl
{
    public PlaylistSidebarView()
    {
        InitializeComponent();
    }

    private void ExportFormatMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: PlaylistSidebarViewModel.PlaylistExportFormatOption option })
        {
            return;
        }

        // Walk up to the parent MenuItem ("Export" submenu) so we can read:
        //  - ItemsControl.Parent chain → the root ContextMenu
        //  - ContextMenu.PlacementTarget.DataContext → the PlaylistSidebarItem that opened the menu
        var parentItem = ItemsControl.ItemsControlFromItemContainer(sender as DependencyObject) as MenuItem;
        if (parentItem?.Tag is not PlaylistSidebarViewModel.PlaylistSidebarItem sidebarItem)
        {
            return;
        }

        if (DataContext is not PlaylistSidebarViewModel vm)
        {
            return;
        }

        var request = new PlaylistSidebarViewModel.PlaylistExportRequest(sidebarItem, option.Format);
        vm.ExportPlaylistCommand.Execute(request);
    }

    private void SidebarItem_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlaylistSidebarViewModel.PlaylistSidebarItem item })
        {
            return;
        }

        if (DataContext is PlaylistSidebarViewModel vm)
        {
            vm.SelectPlaylistCommand.Execute(item);
        }

        if (e.ClickCount == 2 && !item.IsDefault)
        {
            if (DataContext is PlaylistSidebarViewModel vm2)
            {
                vm2.BeginRenameCommand.Execute(item);
            }
        }
    }

    private void SidebarItem_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F2)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: PlaylistSidebarViewModel.PlaylistSidebarItem { IsDefault: false } item }
            && DataContext is PlaylistSidebarViewModel vm)
        {
            vm.BeginRenameCommand.Execute(item);
            e.Handled = true;
        }
    }

    private void RenameTextBox_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    private void RenameTextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: PlaylistSidebarViewModel.PlaylistSidebarItem item }
            && DataContext is PlaylistSidebarViewModel vm)
        {
            vm.CommitRenameCommand.Execute(item);
        }
    }

    private async void ManualPlaylists_OnDrop(object sender, DragEventArgs e)
    {
        // gong-wpf-dragdrop handles the ObservableCollection reorder automatically.
        // We just need to persist the new order after the drop completes.
        if (DataContext is PlaylistSidebarViewModel vm)
        {
            await vm.ReorderPlaylistsCommand.ExecuteAsync(null);
        }
    }

    private void RenameTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: PlaylistSidebarViewModel.PlaylistSidebarItem item }
            || DataContext is not PlaylistSidebarViewModel vm)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                vm.CommitRenameCommand.Execute(item);
                e.Handled = true;
                break;
            case Key.Escape:
                vm.CancelRenameCommand.Execute(item);
                e.Handled = true;
                break;
        }
    }
}
