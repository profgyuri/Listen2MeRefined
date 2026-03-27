namespace Listen2MeRefined.Application.Utils;

/// <summary>
/// Defines viewport normalization and change-detection rules for waveform rendering.
/// </summary>
public interface IWaveformViewportPolicy
{
    /// <summary>
    /// Attempts to normalize raw viewport dimensions into renderable waveform dimensions.
    /// </summary>
    /// <param name="availableWidth">The available viewport width.</param>
    /// <param name="availableHeight">The available viewport height.</param>
    /// <returns>
    /// A normalized width/height tuple when the input is valid; otherwise, <see langword="null" />.
    /// </returns>
    (int Width, int Height)? TryNormalizeViewport(double availableWidth, double availableHeight);

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
    bool HasMeaningfulChange(int currentWidth, int currentHeight, int nextWidth, int nextHeight);
}
