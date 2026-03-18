using Listen2MeRefined.Application.Utils;
using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

/// <summary>
/// Renders waveform bitmaps using the configured waveform drawer.
/// </summary>
public sealed class WaveformRenderer : IWaveformRenderer
{
    private readonly IWaveFormDrawer<SKBitmap> _waveFormDrawer;
    private readonly SemaphoreSlim _renderLock = new(1, 1);

    public WaveformRenderer(IWaveFormDrawer<SKBitmap> waveFormDrawer)
    {
        _waveFormDrawer = waveFormDrawer;
    }

    /// <summary>
    /// Sets the target render size for subsequent waveform draws.
    /// </summary>
    /// <param name="width">The target waveform width.</param>
    /// <param name="height">The target waveform height.</param>
    public void SetSize(int width, int height)
    {
        _waveFormDrawer.SetSize(width, height);
    }

    /// <summary>
    /// Draws a placeholder waveform when no track is available.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel drawing.</param>
    /// <returns>A bitmap containing the placeholder waveform.</returns>
    public async Task<SKBitmap> DrawPlaceholderAsync(CancellationToken cancellationToken = default)
    {
        await _renderLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _waveFormDrawer.LineAsync().ConfigureAwait(false);
        }
        finally
        {
            _renderLock.Release();
        }
    }

    /// <summary>
    /// Draws a waveform for the specified track.
    /// </summary>
    /// <param name="trackPath">The full path to the track.</param>
    /// <param name="cancellationToken">A token that can cancel drawing.</param>
    /// <returns>A bitmap containing the rendered waveform.</returns>
    public async Task<SKBitmap> DrawTrackAsync(string trackPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackPath);

        await _renderLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _waveFormDrawer.WaveFormAsync(trackPath).ConfigureAwait(false);
        }
        finally
        {
            _renderLock.Release();
        }
    }
}
