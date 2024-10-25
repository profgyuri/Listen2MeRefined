namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public interface ICanvas<TPoint, TBitmap>
    where TPoint: struct
    where TBitmap: class
{
    void DrawLine(TPoint p1, TPoint p2, float? strokeWidth = null);
    TBitmap Finish();
    void Reset(int width, int height);
}