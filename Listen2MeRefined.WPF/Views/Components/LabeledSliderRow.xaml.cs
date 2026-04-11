namespace Listen2MeRefined.WPF.Views.Components;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class LabeledSliderRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(LabeledSliderRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelWidthProperty =
        DependencyProperty.Register(
            nameof(LabelWidth),
            typeof(double),
            typeof(LabeledSliderRow),
            new PropertyMetadata(170d));

    public static readonly DependencyProperty SliderWidthProperty =
        DependencyProperty.Register(
            nameof(SliderWidth),
            typeof(double),
            typeof(LabeledSliderRow),
            new PropertyMetadata(140d));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(LabeledSliderRow),
            new PropertyMetadata(0d));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(LabeledSliderRow),
            new PropertyMetadata(100d));

    public static readonly DependencyProperty TickFrequencyProperty =
        DependencyProperty.Register(
            nameof(TickFrequency),
            typeof(double),
            typeof(LabeledSliderRow),
            new PropertyMetadata(1d));

    public static readonly DependencyProperty IsSnapToTickEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSnapToTickEnabled),
            typeof(bool),
            typeof(LabeledSliderRow),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(LabeledSliderRow),
            new FrameworkPropertyMetadata(
                0d,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnDisplayValueInputChanged));

    public static readonly DependencyProperty ValueFormatProperty =
        DependencyProperty.Register(
            nameof(ValueFormat),
            typeof(string),
            typeof(LabeledSliderRow),
            new PropertyMetadata("0", OnDisplayValueInputChanged));

    public static readonly DependencyProperty ValueSuffixProperty =
        DependencyProperty.Register(
            nameof(ValueSuffix),
            typeof(string),
            typeof(LabeledSliderRow),
            new PropertyMetadata(string.Empty, OnDisplayValueInputChanged));

    private static readonly DependencyPropertyKey ValueDisplayPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ValueDisplay),
            typeof(string),
            typeof(LabeledSliderRow),
            new PropertyMetadata("0"));

    public static readonly DependencyProperty ValueDisplayProperty =
        ValueDisplayPropertyKey.DependencyProperty;

    public LabeledSliderRow()
    {
        InitializeComponent();
        UpdateValueDisplay();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double LabelWidth
    {
        get => (double)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public double SliderWidth
    {
        get => (double)GetValue(SliderWidthProperty);
        set => SetValue(SliderWidthProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double TickFrequency
    {
        get => (double)GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public bool IsSnapToTickEnabled
    {
        get => (bool)GetValue(IsSnapToTickEnabledProperty);
        set => SetValue(IsSnapToTickEnabledProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public string ValueSuffix
    {
        get => (string)GetValue(ValueSuffixProperty);
        set => SetValue(ValueSuffixProperty, value);
    }

    public string ValueDisplay => (string)GetValue(ValueDisplayProperty);

    private static void OnDisplayValueInputChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is LabeledSliderRow row)
        {
            row.UpdateValueDisplay();
        }
    }

    private void UpdateValueDisplay()
    {
        var formatted = Value.ToString(ValueFormat, CultureInfo.CurrentCulture);
        SetValue(ValueDisplayPropertyKey, string.Concat(formatted, ValueSuffix));
    }

    private void Slider_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var step = TickFrequency > 0 ? TickFrequency : 1;
        var delta = e.Delta > 0 ? step : -step;
        Value = Math.Clamp(Value + delta, Minimum, Maximum);
        e.Handled = true;
    }
}
