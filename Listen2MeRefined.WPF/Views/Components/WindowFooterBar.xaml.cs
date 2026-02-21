namespace Listen2MeRefined.WPF.Views.Components;

using System.Windows;
using System.Windows.Controls;

public partial class WindowFooterBar : UserControl
{
    public static readonly DependencyProperty InfoTextProperty =
        DependencyProperty.Register(
            nameof(InfoText),
            typeof(string),
            typeof(WindowFooterBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryButtonTextProperty =
        DependencyProperty.Register(
            nameof(PrimaryButtonText),
            typeof(string),
            typeof(WindowFooterBar),
            new PropertyMetadata("OK"));

    public static readonly DependencyProperty SecondaryButtonTextProperty =
        DependencyProperty.Register(
            nameof(SecondaryButtonText),
            typeof(string),
            typeof(WindowFooterBar),
            new PropertyMetadata("Cancel"));

    public static readonly DependencyProperty PrimaryButtonMinWidthProperty =
        DependencyProperty.Register(
            nameof(PrimaryButtonMinWidth),
            typeof(double),
            typeof(WindowFooterBar),
            new PropertyMetadata(110d));

    public static readonly DependencyProperty SecondaryButtonMinWidthProperty =
        DependencyProperty.Register(
            nameof(SecondaryButtonMinWidth),
            typeof(double),
            typeof(WindowFooterBar),
            new PropertyMetadata(110d));

    public static readonly DependencyProperty ShowSecondaryButtonProperty =
        DependencyProperty.Register(
            nameof(ShowSecondaryButton),
            typeof(bool),
            typeof(WindowFooterBar),
            new PropertyMetadata(false));

    public static readonly RoutedEvent PrimaryClickEvent =
        EventManager.RegisterRoutedEvent(
            nameof(PrimaryClick),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(WindowFooterBar));

    public static readonly RoutedEvent SecondaryClickEvent =
        EventManager.RegisterRoutedEvent(
            nameof(SecondaryClick),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(WindowFooterBar));

    public WindowFooterBar()
    {
        InitializeComponent();
    }

    public string InfoText
    {
        get => (string)GetValue(InfoTextProperty);
        set => SetValue(InfoTextProperty, value);
    }

    public string PrimaryButtonText
    {
        get => (string)GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    public string SecondaryButtonText
    {
        get => (string)GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    public double PrimaryButtonMinWidth
    {
        get => (double)GetValue(PrimaryButtonMinWidthProperty);
        set => SetValue(PrimaryButtonMinWidthProperty, value);
    }

    public double SecondaryButtonMinWidth
    {
        get => (double)GetValue(SecondaryButtonMinWidthProperty);
        set => SetValue(SecondaryButtonMinWidthProperty, value);
    }

    public bool ShowSecondaryButton
    {
        get => (bool)GetValue(ShowSecondaryButtonProperty);
        set => SetValue(ShowSecondaryButtonProperty, value);
    }

    public event RoutedEventHandler PrimaryClick
    {
        add => AddHandler(PrimaryClickEvent, value);
        remove => RemoveHandler(PrimaryClickEvent, value);
    }

    public event RoutedEventHandler SecondaryClick
    {
        add => AddHandler(SecondaryClickEvent, value);
        remove => RemoveHandler(SecondaryClickEvent, value);
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(PrimaryClickEvent, this));
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(SecondaryClickEvent, this));
    }
}
