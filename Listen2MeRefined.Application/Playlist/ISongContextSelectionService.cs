using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

public interface ISongContextSelectionService
{
    IReadOnlyList<string> ResolveSearchSelectionPaths(
        IEnumerable<AudioModel> directSelection,
        IEnumerable<AudioModel> fallbackSelection);

    IReadOnlyList<string> ResolvePlaylistSelectionPaths(
        IEnumerable<AudioModel> selectedTabSongs,
        IEnumerable<AudioModel> currentTabSongs,
        AudioModel? selectedSong);
}
