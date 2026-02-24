namespace Listen2MeRefined.WPF.Views.Components;

using System.Windows;
using System.Windows.Controls;

public partial class FormFieldRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(FormFieldRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelWidthProperty =
        DependencyProperty.Register(
            nameof(LabelWidth),
            typeof(double),
            typeof(FormFieldRow),
            new PropertyMetadata(170d));

    public static readonly DependencyProperty FieldContentProperty =
        DependencyProperty.Register(
            nameof(FieldContent),
            typeof(object),
            typeof(FormFieldRow),
            new PropertyMetadata(null));

    public FormFieldRow()
    {
        InitializeComponent();
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

    public object? FieldContent
    {
        get => GetValue(FieldContentProperty);
        set => SetValue(FieldContentProperty, value);
    }
}
