using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public interface IWaveFormDrawer
{
    /// <summary>
    /// Draws the sound wave.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>Bitmap of the sound wave.</returns>
    SKBitmap WaveForm(string path);
    
    /// <summary>
    /// Sets the dimensions of the sound wave.
    /// </summary>
    /// <param name="width">Width of the sound wave.</param>
    /// <param name="height">Height of the sound wave.</param>
    void SetSize(int width, int height);
}