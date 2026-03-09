namespace Listen2MeRefined.WPF.Views.Components;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

public partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(
            nameof(IconKind),
            typeof(PackIconKind),
            typeof(SectionHeader),
            new PropertyMetadata(PackIconKind.MusicNote));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SectionHeader),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(SectionHeader),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShowSubtitleProperty =
        DependencyProperty.Register(
            nameof(ShowSubtitle),
            typeof(bool),
            typeof(SectionHeader),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowActionButtonProperty =
        DependencyProperty.Register(
            nameof(ShowActionButton),
            typeof(bool),
            typeof(SectionHeader),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(SectionHeader),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ActionIconKindProperty =
        DependencyProperty.Register(
            nameof(ActionIconKind),
            typeof(PackIconKind),
            typeof(SectionHeader),
            new PropertyMetadata(PackIconKind.Plus));

    public static readonly DependencyProperty ActionToolTipProperty =
        DependencyProperty.Register(
            nameof(ActionToolTip),
            typeof(string),
            typeof(SectionHeader),
            new PropertyMetadata(string.Empty));

    public SectionHeader()
    {
        InitializeComponent();
    }

    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool ShowSubtitle
    {
        get => (bool)GetValue(ShowSubtitleProperty);
        set => SetValue(ShowSubtitleProperty, value);
    }

    public bool ShowActionButton
    {
        get => (bool)GetValue(ShowActionButtonProperty);
        set => SetValue(ShowActionButtonProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public PackIconKind ActionIconKind
    {
        get => (PackIconKind)GetValue(ActionIconKindProperty);
        set => SetValue(ActionIconKindProperty, value);
    }

    public string ActionToolTip
    {
        get => (string)GetValue(ActionToolTipProperty);
        set => SetValue(ActionToolTipProperty, value);
    }
}
