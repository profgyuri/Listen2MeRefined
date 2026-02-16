namespace Listen2MeRefined.WPF.Utils;

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public static class SelectionChangedCommandBehavior
{
    public static readonly DependencyProperty AddedItemsCommandProperty =
        DependencyProperty.RegisterAttached(
            "AddedItemsCommand",
            typeof(ICommand),
            typeof(SelectionChangedCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty RemovedItemsCommandProperty =
        DependencyProperty.RegisterAttached(
            "RemovedItemsCommand",
            typeof(ICommand),
            typeof(SelectionChangedCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty ScrollSelectedItemIntoViewProperty =
        DependencyProperty.RegisterAttached(
            "ScrollSelectedItemIntoView",
            typeof(bool),
            typeof(SelectionChangedCommandBehavior),
            new PropertyMetadata(false));

    public static void SetAddedItemsCommand(DependencyObject element, ICommand? value) => element.SetValue(AddedItemsCommandProperty, value);
    public static ICommand? GetAddedItemsCommand(DependencyObject element) => (ICommand?)element.GetValue(AddedItemsCommandProperty);

    public static void SetRemovedItemsCommand(DependencyObject element, ICommand? value) => element.SetValue(RemovedItemsCommandProperty, value);
    public static ICommand? GetRemovedItemsCommand(DependencyObject element) => (ICommand?)element.GetValue(RemovedItemsCommandProperty);

    public static void SetScrollSelectedItemIntoView(DependencyObject element, bool value) => element.SetValue(ScrollSelectedItemIntoViewProperty, value);
    public static bool GetScrollSelectedItemIntoView(DependencyObject element) => (bool)element.GetValue(ScrollSelectedItemIntoViewProperty);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Selector selector)
        {
            return;
        }

        selector.SelectionChanged -= Selector_SelectionChanged;

        if (GetAddedItemsCommand(selector) is not null || GetRemovedItemsCommand(selector) is not null)
        {
            selector.SelectionChanged += Selector_SelectionChanged;
        }
    }

    private static void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not Selector selector)
        {
            return;
        }

        var addedItemsCommand = GetAddedItemsCommand(selector);
        var removedItemsCommand = GetRemovedItemsCommand(selector);

        ExecuteCommand(addedItemsCommand, e.AddedItems);
        ExecuteCommand(removedItemsCommand, e.RemovedItems);

        if (!GetScrollSelectedItemIntoView(selector))
        {
            return;
        }

        if (selector is ListView listView && listView.SelectedItem is not null)
        {
            listView.ScrollIntoView(listView.SelectedItem);
        }
    }

    private static void ExecuteCommand(ICommand? command, IList items)
    {
        if (command is null || !command.CanExecute(items))
        {
            return;
        }

        command.Execute(items);
    }
}
