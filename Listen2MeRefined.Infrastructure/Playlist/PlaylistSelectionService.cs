using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class PlaylistSelectionService : IPlaylistSelectionService
{
    /// <inheritdoc />
    public AudioModel[] ResolveSelectedSongs(
        IEnumerable<AudioModel> selectedTabSongs,
        IEnumerable<AudioModel> currentTabSongs,
        AudioModel? selectedSong)
    {
        var currentTabPaths = currentTabSongs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (currentTabPaths.Count == 0)
        {
            return [];
        }

        var selected = selectedTabSongs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Where(x => currentTabPaths.Contains(x.Path!))
            .Distinct()
            .ToArray();

        if (selected.Length > 0)
        {
            return selected;
        }

        if (selectedSong is not null
            && !string.IsNullOrWhiteSpace(selectedSong.Path)
            && currentTabPaths.Contains(selectedSong.Path))
        {
            return [selectedSong];
        }

        return [];
    }
}
