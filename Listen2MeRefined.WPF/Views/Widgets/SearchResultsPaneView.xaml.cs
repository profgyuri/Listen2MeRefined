using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class SearchResultsPaneView : UserControl
{
    public SearchResultsPaneView()
    {
        InitializeComponent();
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
