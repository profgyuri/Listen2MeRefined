using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class SkiaCanvas : IDisposable, ICanvas<SKPoint, SKBitmap>, IWaveformPaletteAware
{
    private static readonly SKColor DefaultWaveLineColor = new(255, 138, 61); // Matches default accent (#FF8A3D).
    private static readonly SKColor DefaultWaveBackgroundColor = new(36, 36, 36); // Matches default dark panel (#242424).

    private SKBitmap? _bitmap;
    private SKCanvas? _canvas;
    private readonly SKPaint _linePaint;
    private SKColor _backgroundColor;

    public SkiaCanvas()
    {
        _linePaint = new SKPaint{
            Color = DefaultWaveLineColor,
            StrokeWidth = 1
        };
        _backgroundColor = DefaultWaveBackgroundColor;
    }
    
    public void DrawLine(SKPoint p1, SKPoint p2, float? stroakWidth = null)
    {
        if (_canvas is null)
        {
            throw new InvalidOperationException("Canvas is not initialized");
        }
        
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
        _canvas?.Flush();
        return _bitmap!;
    }

    public void Reset(int width, int height)
    {
        var oldCanvas = _canvas;
    
        _bitmap = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(_backgroundColor);

        // The old bitmap may still be bound to SKElement and painted by WPF.
        // Its owner (view model) disposes it after the UI swap is complete.
        oldCanvas?.Dispose();
    }

    public void Dispose()
    {
        _canvas?.Dispose();
        _bitmap?.Dispose();
    }

    public void UpdatePalette(SKColor lineColor, SKColor backgroundColor)
    {
        _linePaint.Color = lineColor;
        _backgroundColor = backgroundColor;
    }
}
