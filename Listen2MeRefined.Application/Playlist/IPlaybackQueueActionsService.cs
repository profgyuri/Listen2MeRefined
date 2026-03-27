namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Executes playback actions against the active queue and current selection.
/// </summary>
public interface IPlaybackQueueActionsService
{
    /// <summary>
    /// Gets a value that indicates whether jumping to the selected song is currently valid.
    /// </summary>
    /// <returns><see langword="true" /> if jumping to the selected song is possible; otherwise, <see langword="false" />.</returns>
    bool CanJumpToSelectedSong();

    /// <summary>
    /// Jumps playback to the selected index in the active queue.
    /// </summary>
    /// <returns>A task that represents the asynchronous jump operation.</returns>
    Task JumpToSelectedSongAsync();

    /// <summary>
    /// Moves the selected song to play next in the active queue.
    /// </summary>
    void SetSelectedSongAsNext();

    /// <summary>
    /// Rescans metadata for the selected song and updates queue entries.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation.</returns>
    Task ScanSelectedSongAsync();
}
