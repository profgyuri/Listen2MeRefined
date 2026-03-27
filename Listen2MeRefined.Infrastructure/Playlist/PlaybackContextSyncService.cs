using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Synchronizes playback notifications into shared queue state.
/// </summary>
public sealed class PlaybackContextSyncService : IPlaybackContextSyncService
{
    private readonly IPlaylistQueueState _queueState;

    public PlaybackContextSyncService(IPlaylistQueueState queueState)
    {
        _queueState = queueState;
    }

    /// <inheritdoc />
    public void SetCurrentSong(AudioModel song)
    {
        _queueState.SelectedSong = song;
        _queueState.CurrentSongIndex = _queueState.PlayList.IndexOf(song);

        if (_queueState.CurrentSongIndex >= 0)
        {
            _queueState.SelectedIndex = _queueState.CurrentSongIndex;
        }
    }

    /// <inheritdoc />
    public bool IsSongInActiveQueue(AudioModel? song)
    {
        if (song is null || string.IsNullOrWhiteSpace(song.Path))
        {
            return false;
        }

        return _queueState.PlayList.Any(x =>
            !string.IsNullOrWhiteSpace(x.Path) &&
            x.Path.Equals(song.Path, StringComparison.OrdinalIgnoreCase));
    }
}
