using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Manages mutations of the persistent default playlist.
/// </summary>
public interface IDefaultPlaylistService
{
    /// <summary>
    /// Adds songs to the default playlist while preserving path uniqueness.
    /// </summary>
    /// <param name="songs">The songs to merge into the default playlist.</param>
    void AddSearchResultsToDefaultPlaylist(IEnumerable<AudioModel> songs);

    /// <summary>
    /// Removes songs from the default playlist and from the active queue when default is active.
    /// </summary>
    /// <param name="songs">The songs to remove.</param>
    void RemoveFromDefaultPlaylist(IEnumerable<AudioModel> songs);
}
