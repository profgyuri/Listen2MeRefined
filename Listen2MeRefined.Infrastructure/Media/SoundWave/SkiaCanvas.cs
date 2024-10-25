namespace Listen2MeRefined.Infrastructure.Media.SoundWave;
using SkiaSharp;

public sealed class SkiaCanvas : IDisposable, ICanvas<SKPoint, SKBitmap>
{
    private SKBitmap _bitmap;
    private SKCanvas _canvas;
    private readonly SKPaint _linePaint;
    private readonly SKColor _backgroundColor;

    public SkiaCanvas()
    {
        _linePaint = new SKPaint{
            Color = new SKColor(255, 252, 64),
            StrokeWidth = 1
        };
        _backgroundColor = new SKColor(37, 37, 37);
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

    public void Reset(int width, int height)
    {
        var oldCanvas = _canvas;
        var oldBitmap = _bitmap;
    
        _bitmap = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(_backgroundColor);

        // Dispose old objects after new ones are created successfully
        oldCanvas?.Dispose();
        oldBitmap?.Dispose();
    }

    public void Dispose()
    {
        _canvas?.Dispose();
        _bitmap?.Dispose();
    }
}