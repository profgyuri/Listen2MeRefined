using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class Canvas : IDisposable, ICanvas
{
    private SKBitmap _bitmap;
    private SKCanvas _canvas;
    private readonly SKPaint _linePaint;
    private readonly SKColor _backgroundColor;

    public Canvas()
    {
        _linePaint = new SKPaint{
            Color = new SKColor(232, 255, 56),
            StrokeWidth = 1
        };
        _backgroundColor = new SKColor(50, 50, 64);
    }
    
    public void DrawLine(SKPoint p1, SKPoint p2, float? stroakWidth = null)
    {
        _linePaint.IsAntialias = true;
        _linePaint.Style = SKPaintStyle.Stroke;

        if (stroakWidth is null)
        {
            _canvas.DrawLine(p1, p2, _linePaint);
        }
        else
        {
            var oldStrokeWidth = _linePaint.StrokeWidth;
            _linePaint.StrokeWidth = stroakWidth.Value;
            _canvas.DrawLine(p1, p2, _linePaint);
            _linePaint.StrokeWidth = oldStrokeWidth;
        }
        
    }
    
    public SKBitmap Finish()
    {
        _canvas.Flush();
        return _bitmap;
    }

    /// <inheritdoc />
    public void Reset(int width, int height)
    {
        _bitmap = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(_backgroundColor);
    }

    public void Dispose()
    {
        _canvas.Dispose();
    }
}