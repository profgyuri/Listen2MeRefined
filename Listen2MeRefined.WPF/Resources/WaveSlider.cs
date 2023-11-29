namespace Listen2MeRefined.WPF;
using System.Windows;
using System.Windows.Controls;
using SkiaSharp;

public class WaveSlider : Slider
{
    public static readonly DependencyProperty WaveBitmapProperty =
        DependencyProperty.Register(nameof(WaveBitmap), typeof(SKBitmap), typeof(WaveSlider), new PropertyMetadata(null));

    public SKBitmap WaveBitmap
    {
        get => (SKBitmap)GetValue(WaveBitmapProperty);
        set => SetValue(WaveBitmapProperty, value);
    }
}