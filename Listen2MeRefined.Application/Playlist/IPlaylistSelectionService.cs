using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Resolves playlist-song selections for playlist-pane operations.
/// </summary>
public interface IPlaylistSelectionService
{
    /// <summary>
    /// Resolves the effective playlist-song selection for the active tab.
    /// </summary>
    /// <param name="selectedTabSongs">The currently selected songs in the active tab.</param>
    /// <param name="currentTabSongs">The complete set of songs currently shown in the active tab.</param>
    /// <param name="selectedSong">The currently focused song.</param>
    /// <returns>The songs that should be treated as the active selection.</returns>
    AudioModel[] ResolveSelectedSongs(
        IEnumerable<AudioModel> selectedTabSongs,
        IEnumerable<AudioModel> currentTabSongs,
        AudioModel? selectedSong);
}
