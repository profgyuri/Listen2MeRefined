using Listen2MeRefined.Infrastructure.SystemOperations;
using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class WaveFormDrawer
    : IWaveFormDrawer
{
    private int _height;
    private int _width;
    private readonly IFileReader _fileReader;
    private readonly IPeakProvider _peakProvider;
    private readonly ICanvas _canvas;

    /// <summary>
    /// Class used to draw the sound wave.
    /// </summary>
    public WaveFormDrawer(
        IFileReader fileReader,
        IPeakProvider peakProvider,
        ICanvas canvas)
    {
        _fileReader = fileReader;
        _peakProvider = peakProvider;
        _canvas = canvas;
    }

    /// <summary>
    /// Draws the sound wave.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>Bitmap of the sound wave.</returns>
    public async Task<SKBitmap> WaveFormAsync(string path)
    {
        _fileReader.Open(path);
        _fileReader.SetSampleCount(_width);
        _peakProvider.SetReader(_fileReader);
        var peaks = await _peakProvider.GetAllPeaksAsync(_width);

        var midPoint = _height / 2;
        _canvas.Reset(_width, _height);

        for (var i = 0; i < peaks.Length; i++)
        {
            var lineHeight = peaks[i] * midPoint;
            var point1 = new SKPoint(i, midPoint + lineHeight);
            var point2 = new SKPoint(i, midPoint - lineHeight);
            
            _canvas.DrawLine(point1, point2);
        }
        
        return _canvas.Finish();
    }

    /// <inheritdoc />
    public async Task<SKBitmap> LineAsync()
    {
        await Task.Run(() => _canvas.Reset(_width, _height));
        
        var p1 = new SKPoint(0, (float) _height / 2);
        var p2 = new SKPoint(_width, (float) _height / 2);
        _canvas.DrawLine(p1, p2, 6);
        
        return _canvas.Finish();
    }

    /// <inheritdoc />
    public void SetSize(
        int width,
        int height)
    {
        var scale = Resolution.GetScaleFactor();
        _height = (int)(height * scale);
        _width = (int)(width * scale);
    }
}