using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class Canvas : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly SKPaint _paint;
    
    public Canvas(int width, int height)
    {
        _bitmap = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(new SKColor(50, 50, 64));
        _paint = new SKPaint{
            Color = SKColors.Orange,
            StrokeWidth = 1
        };
    }
    
    public void DrawLine(SKPoint p1, SKPoint p2)
    {
        _paint.IsAntialias = true;
        _paint.Style = SKPaintStyle.Stroke;
        _canvas.DrawLine(p1, p2, _paint);
    }
    
    public SKBitmap Finish()
    {
        _canvas.Flush();
        return _bitmap;
    }

    public void Dispose()
    {
        _canvas.Dispose();
    }
}