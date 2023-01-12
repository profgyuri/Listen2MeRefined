using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class Drawing
{
    private readonly int _height;
    private readonly int _width;

    /// <summary>
    /// Class used to draw the sound wave.
    /// </summary>
    /// <param name="height">Height of the drawing.</param>
    /// <param name="width">Width of the drawing.</param>
    public Drawing(int height,
        int width)
    {
        _height = height;
        _width = width;
    }

    /// <summary>
    /// Draws the sound wave.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>Bitmap of the sound wave.</returns>
    public SKBitmap WaveForm(string path)
    {
        var reader = new FileReader(path, _width);
        var peakProvider = new PeakProvider(reader);
        var peaks = peakProvider.GetAllPeaks(_width);

        var midPoint = _height / 2;
        using var canvas = new Canvas(_width, _height);

        for (var i = 0; i < peaks.Length; i++)
        {
            var lineHeight = peaks[i] * midPoint;
            var point1 = new SKPoint(i, midPoint + lineHeight);
            var point2 = new SKPoint(i, midPoint - lineHeight);
            
            canvas.DrawLine(point1, point2);
        }
        
        return canvas.Finish();
    }
}