using System.Windows;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace Listen2MeRefined.WPF;

internal static class SkiaElementHelper
{
    public static readonly DependencyProperty BitmapProperty =
        DependencyProperty.RegisterAttached(
            "Bitmap",
            typeof(SKBitmap),
            typeof(SkiaElementHelper),
            new PropertyMetadata(null, OnBitmapChanged));

    public static void SetBitmap(SKElement element, SKBitmap value)
    {
        element.SetValue(BitmapProperty, value);
    }

    public static SKBitmap GetBitmap(SKElement element)
    {
        return (SKBitmap)element.GetValue(BitmapProperty);
    }

    private static void OnBitmapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var element = (SKElement)d;
        element.PaintSurface -= OnPaintSurface;
        element.PaintSurface += OnPaintSurface;
        element.InvalidateVisual();
    }

    private static void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var element = (SKElement)sender;
        var bitmap = (SKBitmap)element.GetValue(BitmapProperty);
        if (bitmap is not null)
        {
            e.Surface.Canvas.DrawBitmap(bitmap, 0, 0);
        }
    }
}