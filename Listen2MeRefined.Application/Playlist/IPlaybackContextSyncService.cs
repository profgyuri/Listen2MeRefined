using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Synchronizes playback notifications into shared queue state.
/// </summary>
public interface IPlaybackContextSyncService
{
    /// <summary>
    /// Updates shared queue state based on the latest current-song notification.
    /// </summary>
    /// <param name="song">The current playback song.</param>
    void SetCurrentSong(AudioModel song);

    /// <summary>
    /// Gets a value that indicates whether the song belongs to the active queue.
    /// </summary>
    /// <param name="song">The song to check.</param>
    /// <returns><see langword="true" /> if the song is in the active queue; otherwise, <see langword="false" />.</returns>
    bool IsSongInActiveQueue(AudioModel? song);
}
