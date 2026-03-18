using SkiaSharp;

namespace Listen2MeRefined.Application.Utils;

/// <summary>
/// Draws waveform visuals for the playback UI.
/// </summary>
public interface IWaveformRenderer
{
    /// <summary>
    /// Sets the target render size for subsequent waveform draws.
    /// </summary>
    /// <param name="width">The target waveform width.</param>
    /// <param name="height">The target waveform height.</param>
    void SetSize(int width, int height);

    /// <summary>
    /// Draws a placeholder waveform when no track is available.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel drawing.</param>
    /// <returns>A bitmap containing the placeholder waveform.</returns>
    Task<SKBitmap> DrawPlaceholderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Draws a waveform for the specified track.
    /// </summary>
    /// <param name="trackPath">The full path to the track.</param>
    /// <param name="cancellationToken">A token that can cancel drawing.</param>
    /// <returns>A bitmap containing the rendered waveform.</returns>
    Task<SKBitmap> DrawTrackAsync(string trackPath, CancellationToken cancellationToken = default);
}
