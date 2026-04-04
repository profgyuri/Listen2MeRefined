using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Resolves normalized song-path selections for context-menu operations.
/// </summary>
public interface ISongContextSelectionService
{
    /// <summary>
    /// Resolves a unified selection path set from direct, fallback, and focused-song sources.
    /// </summary>
    /// <param name="directSelection">A directly selected set of songs.</param>
    /// <param name="fallbackSelection">A fallback set of songs used to scope the selection (empty to skip scoping).</param>
    /// <param name="focusedSong">The currently focused single song, used as a last-resort fallback.</param>
    /// <returns>A normalized collection of selected song paths.</returns>
    IReadOnlyList<string> ResolveSelectionPaths(
        IEnumerable<AudioModel> directSelection,
        IEnumerable<AudioModel> fallbackSelection,
        AudioModel? focusedSong);
}
