namespace Listen2MeRefined.WPF.Views.Components;

using System.Windows;
using System.Windows.Controls;

public partial class MetaBadgeRow : UserControl
{
    public static readonly DependencyProperty BpmTextProperty =
        DependencyProperty.Register(
            nameof(BpmText),
            typeof(string),
            typeof(MetaBadgeRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BitrateTextProperty =
        DependencyProperty.Register(
            nameof(BitrateText),
            typeof(string),
            typeof(MetaBadgeRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LengthTextProperty =
        DependencyProperty.Register(
            nameof(LengthText),
            typeof(string),
            typeof(MetaBadgeRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GenreTextProperty =
        DependencyProperty.Register(
            nameof(GenreText),
            typeof(string),
            typeof(MetaBadgeRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShowBpmProperty =
        DependencyProperty.Register(
            nameof(ShowBpm),
            typeof(bool),
            typeof(MetaBadgeRow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowBitrateProperty =
        DependencyProperty.Register(
            nameof(ShowBitrate),
            typeof(bool),
            typeof(MetaBadgeRow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowLengthProperty =
        DependencyProperty.Register(
            nameof(ShowLength),
            typeof(bool),
            typeof(MetaBadgeRow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowGenreProperty =
        DependencyProperty.Register(
            nameof(ShowGenre),
            typeof(bool),
            typeof(MetaBadgeRow),
            new PropertyMetadata(true));

    public MetaBadgeRow()
    {
        InitializeComponent();
    }

    public string BpmText
    {
        get => (string)GetValue(BpmTextProperty);
        set => SetValue(BpmTextProperty, value);
    }

    public string BitrateText
    {
        get => (string)GetValue(BitrateTextProperty);
        set => SetValue(BitrateTextProperty, value);
    }

    public string LengthText
    {
        get => (string)GetValue(LengthTextProperty);
        set => SetValue(LengthTextProperty, value);
    }

    public string GenreText
    {
        get => (string)GetValue(GenreTextProperty);
        set => SetValue(GenreTextProperty, value);
    }

    public bool ShowBpm
    {
        get => (bool)GetValue(ShowBpmProperty);
        set => SetValue(ShowBpmProperty, value);
    }

    public bool ShowBitrate
    {
        get => (bool)GetValue(ShowBitrateProperty);
        set => SetValue(ShowBitrateProperty, value);
    }

    public bool ShowLength
    {
        get => (bool)GetValue(ShowLengthProperty);
        set => SetValue(ShowLengthProperty, value);
    }

    public bool ShowGenre
    {
        get => (bool)GetValue(ShowGenreProperty);
        set => SetValue(ShowGenreProperty, value);
    }
}
