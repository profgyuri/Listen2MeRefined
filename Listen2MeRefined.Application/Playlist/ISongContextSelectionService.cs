using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Resolves normalized song-path selections for context-menu operations.
/// </summary>
public interface ISongContextSelectionService
{
    /// <summary>
    /// Resolves a search-selection path set from direct and fallback selections.
    /// </summary>
    /// <param name="directSelection">A directly selected set of songs.</param>
    /// <param name="fallbackSelection">A fallback set of songs used when direct selection is empty.</param>
    /// <returns>A normalized collection of selected song paths.</returns>
    IReadOnlyList<string> ResolveSearchSelectionPaths(
        IEnumerable<AudioModel> directSelection,
        IEnumerable<AudioModel> fallbackSelection);

    /// <summary>
    /// Resolves a playlist-selection path set for context-menu actions.
    /// </summary>
    /// <param name="selectedTabSongs">A selected set of songs in the current tab.</param>
    /// <param name="currentTabSongs">A complete set of songs in the current tab.</param>
    /// <param name="selectedSong">The currently focused song.</param>
    /// <returns>A normalized collection of selected song paths.</returns>
    IReadOnlyList<string> ResolvePlaylistSelectionPaths(
        IEnumerable<AudioModel> selectedTabSongs,
        IEnumerable<AudioModel> currentTabSongs,
        AudioModel? selectedSong);
}
