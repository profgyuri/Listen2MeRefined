namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Provides operations that drive song context-menu playlist actions.
/// </summary>
public interface IPlaylistMembership
{
    /// <summary>
    /// Gets playlist membership states for the current song selection.
    /// </summary>
    /// <param name="selectedSongPaths">A collection of selected song paths.</param>
    /// <param name="activePlaylistId">The active playlist identifier, if any.</param>
    /// <param name="ct">A token that can cancel the asynchronous operation.</param>
    /// <returns>A collection of playlist membership states for the context menu.</returns>
    Task<IReadOnlyList<PlaylistMembershipInfo>> GetPlaylistMembershipInfoAsync(
        IReadOnlyList<string> selectedSongPaths,
        int? activePlaylistId,
        CancellationToken ct = default);

    /// <summary>
    /// Adds or removes the current song selection to or from a playlist.
    /// </summary>
    /// <param name="playlistId">The target playlist identifier.</param>
    /// <param name="selectedSongPaths">A collection of selected song paths.</param>
    /// <param name="shouldContain"><see langword="true" /> to ensure the playlist contains the songs; otherwise, <see langword="false" />.</param>
    /// <param name="allowRemove"><see langword="true" /> to allow removal when <paramref name="shouldContain" /> is <see langword="false" />; otherwise, <see langword="false" />.</param>
    /// <param name="ct">A token that can cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task TogglePlaylistMembershipAsync(
        int playlistId,
        IReadOnlyList<string> selectedSongPaths,
        bool shouldContain,
        bool allowRemove,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new playlist and adds the current song selection to it.
    /// </summary>
    /// <param name="playlistName">A playlist name.</param>
    /// <param name="selectedSongPaths">A collection of selected song paths.</param>
    /// <param name="ct">A token that can cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddToNewPlaylistAsync(
        string playlistName,
        IReadOnlyList<string> selectedSongPaths,
        CancellationToken ct = default);
}
