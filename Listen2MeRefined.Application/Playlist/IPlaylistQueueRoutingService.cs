using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Routes playback between default and named playlist queues.
/// </summary>
public interface IPlaylistQueueRoutingService
{
    /// <summary>
    /// Activates the default playlist as the playback queue.
    /// </summary>
    void ActivateDefaultPlaylistQueue();

    /// <summary>
    /// Activates a named playlist as the playback queue.
    /// </summary>
    /// <param name="playlistId">The named playlist identifier.</param>
    /// <param name="songs">The songs belonging to the named playlist.</param>
    void ActivateNamedPlaylistQueue(int playlistId, IEnumerable<AudioModel> songs);

    /// <summary>
    /// Switches to the default queue while preserving the current song when possible.
    /// </summary>
    /// <returns><see langword="true" /> if the current song was preserved; otherwise, <see langword="false" />.</returns>
    bool SwitchActiveQueueToDefaultPreservingCurrentSong();

    /// <summary>
    /// Switches to the default queue and stops playback.
    /// </summary>
    void SwitchActiveQueueToDefaultAndStop();

    /// <summary>
    /// Syncs default playlist order to match the active queue order.
    /// </summary>
    void SyncDefaultPlaylistOrder();
}
