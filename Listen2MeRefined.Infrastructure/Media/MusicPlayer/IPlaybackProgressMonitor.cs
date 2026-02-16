namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

/// <summary>
/// Evaluates playback progress and determines when a track should automatically advance.
/// </summary>
public interface IPlaybackProgressMonitor
{
    /// <summary>
    /// Resets monitor internals when playback state or active track changes.
    /// </summary>
    void Reset();

    /// <summary>
    /// Evaluates the current playback position and determines whether the player should advance to the next track.
    /// </summary>
    /// <param name="currentTime">The current playback position.</param>
    /// <param name="totalTime">The total track duration.</param>
    /// <param name="isPlaying">Whether the player is currently in a playing state.</param>
    /// <returns><c>true</c> if playback should advance to the next track; otherwise, <c>false</c>.</returns>
    bool ShouldAdvance(TimeSpan currentTime, TimeSpan totalTime, bool isPlaying);
}
