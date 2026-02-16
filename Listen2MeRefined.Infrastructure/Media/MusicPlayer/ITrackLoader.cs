namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

/// <summary>
/// Loads tracks into playable audio readers and returns structured failure information when loading fails.
/// </summary>
public interface ITrackLoader
{
    /// <summary>
    /// Attempts to load the given track and create a <see cref="NAudio.Wave.WaveStream"/> reader.
    /// </summary>
    /// <param name="track">The track to load.</param>
    /// <returns>A structured load result with success state, reader, and optional failure reason.</returns>
    TrackLoadResult Load(AudioModel track);
}
