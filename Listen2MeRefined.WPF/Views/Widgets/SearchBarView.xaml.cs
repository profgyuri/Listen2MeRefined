using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;

namespace Listen2MeRefined.WPF.Views.Widgets;

public partial class SearchBarView : UserControl
{
    public SearchBarView()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<FocusSearchBarRequestedMessage>(this, (_, _) =>
        {
            Dispatcher.Invoke(() => SearchTextBox.Focus());
        });

        Unloaded += (_, _) => WeakReferenceMessenger.Default.Unregister<FocusSearchBarRequestedMessage>(this);
    }

    private void SearchTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SearchTextBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            e.Handled = true;
        }
    }
}
