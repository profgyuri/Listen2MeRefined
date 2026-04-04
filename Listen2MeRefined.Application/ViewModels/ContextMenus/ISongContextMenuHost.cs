using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.ViewModels.ContextMenus;

/// <summary>
/// Contract for ViewModels that host a <see cref="SongContextMenuViewModel"/>.
/// </summary>
public interface ISongContextMenuHost
{
    /// <summary>
    /// Returns the songs that were explicitly selected by the user (e.g. multi-select).
    /// </summary>
    IReadOnlyCollection<AudioModel> GetDirectSongContextSelection();

    /// <summary>
    /// Returns a fallback set of songs when there is no explicit selection
    /// (e.g. all songs in the current tab, or empty for search results).
    /// </summary>
    IReadOnlyCollection<AudioModel> GetFallbackSongContextSelection();

    /// <summary>
    /// Returns the currently focused single song, if any.
    /// </summary>
    AudioModel? GetFocusedSong();

    /// <summary>
    /// Returns the active named playlist id, or <c>null</c> when on the default playlist.
    /// </summary>
    int? GetSongContextActivePlaylistId();

    /// <summary>
    /// Whether playlist membership actions (add/remove from playlists) should be shown for this host.
    /// </summary>
    bool ShowPlaylistMembershipActions { get; }

    /// <summary>
    /// Whether the "Remove from playlist" action should be shown (only on default playlist tab).
    /// </summary>
    bool ShowRemoveFromPlaylistAction { get; }

    /// <summary>
    /// Whether the "Add to default playlist" action should be shown.
    /// </summary>
    bool ShowAddToDefaultPlaylistAction { get; }

    /// <summary>
    /// Whether playback actions (Play Now, Play After Current) are available from this host.
    /// </summary>
    bool ArePlaybackActionsAvailable { get; }
}
