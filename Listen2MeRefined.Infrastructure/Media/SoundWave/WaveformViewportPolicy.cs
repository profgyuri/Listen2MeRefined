using Listen2MeRefined.Application.Utils;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

/// <summary>
/// Provides normalization and change-detection rules for waveform viewport dimensions.
/// </summary>
public sealed class WaveformViewportPolicy : IWaveformViewportPolicy
{
    private const int MinimumWaveFormWidth = 64;
    private const int MinimumWaveFormHeight = 24;
    private const int ResizeNoiseThreshold = 2;

    /// <summary>
    /// Attempts to normalize raw viewport dimensions into renderable waveform dimensions.
    /// </summary>
    /// <param name="availableWidth">The available viewport width.</param>
    /// <param name="availableHeight">The available viewport height.</param>
    /// <returns>
    /// A normalized width/height tuple when the input is valid; otherwise, <see langword="null" />.
    /// </returns>
    public (int Width, int Height)? TryNormalizeViewport(double availableWidth, double availableHeight)
    {
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
        {
            return null;
        }

        if (double.IsNaN(availableHeight) || double.IsInfinity(availableHeight) || availableHeight <= 0)
        {
            return null;
        }

        var width = Math.Max(MinimumWaveFormWidth, (int)Math.Round(availableWidth));
        var height = Math.Max(MinimumWaveFormHeight, (int)Math.Round(availableHeight));
        return (width, height);
    }

    /// <summary>
    /// Determines whether a viewport size change is meaningful enough to trigger a redraw.
    /// </summary>
    /// <param name="currentWidth">The current waveform width.</param>
    /// <param name="currentHeight">The current waveform height.</param>
    /// <param name="nextWidth">The next waveform width.</param>
    /// <param name="nextHeight">The next waveform height.</param>
    /// <returns>
    /// <see langword="true" /> when the change is meaningful; otherwise, <see langword="false" />.
    /// </returns>
    public bool HasMeaningfulChange(int currentWidth, int currentHeight, int nextWidth, int nextHeight)
    {
        return Math.Abs(currentWidth - nextWidth) > ResizeNoiseThreshold
               || Math.Abs(currentHeight - nextHeight) > ResizeNoiseThreshold;
    }
}
