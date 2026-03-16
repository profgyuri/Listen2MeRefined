using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Handles mutations of the default playlist collection.
/// </summary>
public sealed class DefaultPlaylistService : IDefaultPlaylistService
{
    private readonly IPlaylistQueueState _queueState;

    public DefaultPlaylistService(IPlaylistQueueState queueState)
    {
        _queueState = queueState;
    }

    /// <inheritdoc />
    public void AddSearchResultsToDefaultPlaylist(IEnumerable<AudioModel> songs)
    {
        var existingPaths = _queueState.DefaultPlaylist
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var song in songs)
        {
            if (string.IsNullOrWhiteSpace(song.Path) || existingPaths.Contains(song.Path))
            {
                continue;
            }

            _queueState.DefaultPlaylist.Add(song);
            existingPaths.Add(song.Path);

            if (_queueState.IsDefaultPlaylistActive &&
                !_queueState.PlayList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.Path) &&
                    x.Path!.Equals(song.Path, StringComparison.OrdinalIgnoreCase)))
            {
                _queueState.PlayList.Add(song);
            }
        }
    }

    /// <inheritdoc />
    public void RemoveFromDefaultPlaylist(IEnumerable<AudioModel> songs)
    {
        var candidates = songs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (candidates.Count == 0)
        {
            return;
        }

        var toRemove = _queueState.DefaultPlaylist
            .Where(x => !string.IsNullOrWhiteSpace(x.Path) && candidates.Contains(x.Path!))
            .ToArray();

        foreach (var song in toRemove)
        {
            _queueState.DefaultPlaylist.Remove(song);
            if (_queueState.IsDefaultPlaylistActive)
            {
                _queueState.PlayList.Remove(song);
            }
        }
    }
}
