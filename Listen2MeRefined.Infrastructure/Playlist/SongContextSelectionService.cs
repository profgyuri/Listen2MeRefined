using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class SongContextSelectionService : ISongContextSelectionService
{
    public IReadOnlyList<string> ResolveSearchSelectionPaths(
        IEnumerable<AudioModel> directSelection,
        IEnumerable<AudioModel> fallbackSelection)
    {
        var direct = NormalizePaths(directSelection);
        if (direct.Count > 0)
        {
            return direct;
        }

        return NormalizePaths(fallbackSelection);
    }

    public IReadOnlyList<string> ResolvePlaylistSelectionPaths(
        IEnumerable<AudioModel> selectedTabSongs,
        IEnumerable<AudioModel> currentTabSongs,
        AudioModel? selectedSong)
    {
        var currentTabPaths = NormalizePaths(currentTabSongs).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (currentTabPaths.Count == 0)
        {
            return [];
        }

        var selected = NormalizePaths(selectedTabSongs)
            .Where(currentTabPaths.Contains)
            .ToArray();

        if (selected.Length > 0)
        {
            return selected;
        }

        if (selectedSong is null || string.IsNullOrWhiteSpace(selectedSong.Path))
        {
            return [];
        }

        var selectedPath = selectedSong.Path.Trim();
        return currentTabPaths.Contains(selectedPath) ? [selectedPath] : [];
    }

    private static IReadOnlyList<string> NormalizePaths(IEnumerable<AudioModel> songs)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var song in songs)
        {
            if (song is null || string.IsNullOrWhiteSpace(song.Path))
            {
                continue;
            }

            paths.Add(song.Path.Trim());
        }

        return paths.ToArray();
    }
}
