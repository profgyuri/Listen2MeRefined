namespace Listen2MeRefined.WPF.Utils;

using System.Windows;
using Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

public static class WaveformViewportSyncBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(WaveformViewportSyncBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= OnElementLoaded;
        element.SizeChanged -= OnElementSizeChanged;
        element.DataContextChanged -= OnElementDataContextChanged;

        if (!(bool)e.NewValue)
        {
            return;
        }

        element.Loaded += OnElementLoaded;
        element.SizeChanged += OnElementSizeChanged;
        element.DataContextChanged += OnElementDataContextChanged;
        PublishViewport(element);
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        PublishViewport((FrameworkElement)sender);
    }

    private static void OnElementSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged && !e.HeightChanged)
        {
            return;
        }

        PublishViewport((FrameworkElement)sender);
    }

    private static void OnElementDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        PublishViewport((FrameworkElement)sender);
    }

    private static void PublishViewport(FrameworkElement element)
    {
        if (!GetIsEnabled(element))
        {
            return;
        }

        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return;
        }

        if (element.DataContext is not IWaveformViewportAware viewportAware)
        {
            return;
        }

        viewportAware.UpdateWaveformViewport(element.ActualWidth, element.ActualHeight);
    }
}
