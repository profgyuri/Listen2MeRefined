using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public interface ICanvas
{
    void DrawLine(SKPoint p1, SKPoint p2, float? strokeWidth = null);
    SKBitmap Finish();
    void Reset(int width, int height);
}