using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class SongContextSelectionService : ISongContextSelectionService
{
    public IReadOnlyList<string> ResolveSelectionPaths(
        IEnumerable<AudioModel> directSelection,
        IEnumerable<AudioModel> fallbackSelection,
        AudioModel? focusedSong)
    {
        var direct = NormalizePaths(directSelection);
        var scope = NormalizePaths(fallbackSelection).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // When a scope set exists (e.g. current tab songs), filter direct selection against it.
        if (scope.Count > 0)
        {
            var scoped = direct.Where(scope.Contains).ToArray();
            if (scoped.Length > 0)
            {
                return scoped;
            }
        }
        else if (direct.Count > 0)
        {
            return direct;
        }

        // Fall back to the focused song.
        if (focusedSong is null || string.IsNullOrWhiteSpace(focusedSong.Path))
        {
            return [];
        }

        var focusedPath = focusedSong.Path.Trim();
        return scope.Count == 0 || scope.Contains(focusedPath) ? [focusedPath] : [];
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
