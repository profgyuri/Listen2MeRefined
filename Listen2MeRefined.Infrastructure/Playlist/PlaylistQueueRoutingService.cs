using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Routes playback queue between default and named playlist sources.
/// </summary>
public sealed class PlaylistQueueRoutingService : IPlaylistQueueRoutingService
{
    private readonly IPlaylistQueueState _queueState;
    private readonly IPlaylistQueue _playList;
    private readonly IMusicPlayerController _musicPlayerController;

    public PlaylistQueueRoutingService(
        IPlaylistQueueState queueState,
        IPlaylistQueue playList,
        IMusicPlayerController musicPlayerController)
    {
        _queueState = queueState;
        _playList = playList;
        _musicPlayerController = musicPlayerController;
    }

    /// <inheritdoc />
    public void ActivateDefaultPlaylistQueue()
    {
        ReplacePlaybackQueue(_queueState.DefaultPlaylist);
        _queueState.SetActiveNamedPlaylistId(null);
    }

    /// <inheritdoc />
    public void ActivateNamedPlaylistQueue(int playlistId, IEnumerable<AudioModel> songs)
    {
        ReplacePlaybackQueue(songs);
        _queueState.SetActiveNamedPlaylistId(playlistId);
    }

    /// <inheritdoc />
    public bool SwitchActiveQueueToDefaultPreservingCurrentSong()
    {
        var currentSongPath = _queueState.SelectedSong?.Path;
        ReplacePlaybackQueue(_queueState.DefaultPlaylist);
        _queueState.SetActiveNamedPlaylistId(null);

        var index = IndexOfPath(_queueState.PlayList, currentSongPath);
        if (index < 0)
        {
            return false;
        }

        _playList.CurrentIndex = index;
        _queueState.SelectedIndex = index;
        _queueState.CurrentSongIndex = index;
        return true;
    }

    /// <inheritdoc />
    public void SwitchActiveQueueToDefaultAndStop()
    {
        ReplacePlaybackQueue(_queueState.DefaultPlaylist);
        _queueState.SetActiveNamedPlaylistId(null);
        _musicPlayerController.Stop();
    }

    /// <inheritdoc />
    public void SyncDefaultPlaylistOrder()
    {
        var target = _playList.Items;
        for (int i = 0; i < target.Count; i++)
        {
            var currentPos = _queueState.DefaultPlaylist.IndexOf(target[i]);
            if (currentPos >= 0 && currentPos != i)
            {
                _queueState.DefaultPlaylist.Move(currentPos, i);
            }
        }
    }

    /// <summary>
    /// Replaces the active playback queue with the provided songs and realigns shared selection/index state.
    /// </summary>
    /// <param name="songs">The songs that should become the active queue.</param>
    private void ReplacePlaybackQueue(IEnumerable<AudioModel> songs)
    {
        var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueSongs = songs
            .Where(x => !string.IsNullOrWhiteSpace(x.Path) && uniquePaths.Add(x.Path!))
            .ToArray();

        _queueState.PlayList.Clear();
        foreach (var song in uniqueSongs)
        {
            _queueState.PlayList.Add(song);
        }

        if (_queueState.PlayList.Count == 0)
        {
            _playList.CurrentIndex = 0;
            _queueState.CurrentSongIndex = -1;
            _queueState.SelectedIndex = -1;
            return;
        }

        var currentPath = _queueState.SelectedSong?.Path;
        var matchingIndex = IndexOfPath(_queueState.PlayList, currentPath);
        if (matchingIndex >= 0)
        {
            _playList.CurrentIndex = matchingIndex;
            _queueState.CurrentSongIndex = matchingIndex;
            _queueState.SelectedIndex = matchingIndex;
            return;
        }

        _playList.CurrentIndex = 0;
        _queueState.CurrentSongIndex = 0;
        _queueState.SelectedIndex = 0;
    }

    private static int IndexOfPath(IEnumerable<AudioModel> songs, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return -1;
        }

        var index = 0;
        foreach (var song in songs)
        {
            if (!string.IsNullOrWhiteSpace(song.Path) &&
                song.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index++;
        }

        return -1;
    }
}
