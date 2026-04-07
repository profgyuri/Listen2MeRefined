namespace Listen2MeRefined.WPF.Views.Components;

using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

public partial class EmptyStateOverlay : UserControl
{
    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(
            nameof(IconKind),
            typeof(PackIconKind),
            typeof(EmptyStateOverlay),
            new PropertyMetadata(PackIconKind.InformationOutline));

    public static readonly DependencyProperty PrimaryTextProperty =
        DependencyProperty.Register(
            nameof(PrimaryText),
            typeof(string),
            typeof(EmptyStateOverlay),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SecondaryTextProperty =
        DependencyProperty.Register(
            nameof(SecondaryText),
            typeof(string),
            typeof(EmptyStateOverlay),
            new PropertyMetadata(string.Empty, OnSecondaryTextChanged));

    public static readonly DependencyProperty ShowSecondaryTextProperty =
        DependencyProperty.Register(
            nameof(ShowSecondaryText),
            typeof(bool),
            typeof(EmptyStateOverlay),
            new PropertyMetadata(false));

    public EmptyStateOverlay()
    {
        InitializeComponent();
    }

    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public string PrimaryText
    {
        get => (string)GetValue(PrimaryTextProperty);
        set => SetValue(PrimaryTextProperty, value);
    }

    public string SecondaryText
    {
        get => (string)GetValue(SecondaryTextProperty);
        set => SetValue(SecondaryTextProperty, value);
    }

    public bool ShowSecondaryText
    {
        get => (bool)GetValue(ShowSecondaryTextProperty);
        set => SetValue(ShowSecondaryTextProperty, value);
    }

    private static void OnSecondaryTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EmptyStateOverlay overlay)
        {
            overlay.ShowSecondaryText = !string.IsNullOrEmpty((string)e.NewValue);
        }
    }
}
