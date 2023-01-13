namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public interface IWaveFormDrawer<TBitmap> 
    where TBitmap : class
{
    /// <summary>
    /// Draws the sound wave.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>Bitmap of the sound wave.</returns>
    Task<TBitmap> WaveFormAsync(string path);

    /// <summary>
    /// Draws a placeholder line for the sound wave.
    /// </summary>
    /// <returns></returns>
    Task<TBitmap> LineAsync();

    /// <summary>
    /// Sets the dimensions of the sound wave.
    /// </summary>
    /// <param name="width">Width of the sound wave.</param>
    /// <param name="height">Height of the sound wave.</param>
    void SetSize(int width, int height);
}