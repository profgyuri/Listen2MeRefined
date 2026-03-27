using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Executes playback actions against the current queue selection.
/// </summary>
public sealed class PlaybackQueueActionsService : IPlaybackQueueActionsService
{
    private readonly IPlaylistQueueState _queueState;
    private readonly IPlaybackContextSyncService _playbackContextSyncService;
    private readonly IFileScanner _fileScanner;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly ILogger _logger;

    public PlaybackQueueActionsService(
        IPlaylistQueueState queueState,
        IPlaybackContextSyncService playbackContextSyncService,
        IFileScanner fileScanner,
        IMusicPlayerController musicPlayerController,
        ILogger logger)
    {
        _queueState = queueState;
        _playbackContextSyncService = playbackContextSyncService;
        _fileScanner = fileScanner;
        _musicPlayerController = musicPlayerController;
        _logger = logger.ForContext(GetType());
    }

    /// <inheritdoc />
    public bool CanJumpToSelectedSong()
    {
        return _queueState.SelectedIndex > -1 &&
               _playbackContextSyncService.IsSongInActiveQueue(_queueState.SelectedSong);
    }

    /// <inheritdoc />
    public async Task JumpToSelectedSongAsync()
    {
        if (!CanJumpToSelectedSong())
        {
            return;
        }

        _logger.Debug("[PlaybackQueueActionsService] Jumping to selected index {Index}", _queueState.SelectedIndex);
        await _musicPlayerController.JumpToIndexAsync(_queueState.SelectedIndex);
    }

    /// <inheritdoc />
    public void SetSelectedSongAsNext()
    {
        var selectedSong = _queueState.SelectedSong;
        if (selectedSong is null ||
            _queueState.PlayList.Count <= 1 ||
            !_playbackContextSyncService.IsSongInActiveQueue(selectedSong))
        {
            return;
        }

        var currentSongIndex = _queueState.CurrentSongIndex;
        if (currentSongIndex < 0 || currentSongIndex >= _queueState.PlayList.Count)
        {
            return;
        }

        var selectedSongIndex = IndexOfSongByPath(_queueState.PlayList, selectedSong.Path);
        if (selectedSongIndex < 0)
        {
            return;
        }

        selectedSong = _queueState.PlayList[selectedSongIndex];
        var currentSong = _queueState.PlayList[currentSongIndex];
        if (ReferenceEquals(selectedSong, currentSong))
        {
            return;
        }

        _queueState.PlayList.RemoveAt(selectedSongIndex);

        var updatedCurrentSongIndex = _queueState.PlayList.IndexOf(currentSong);
        if (updatedCurrentSongIndex < 0)
        {
            return;
        }

        var insertionIndex = updatedCurrentSongIndex + 1;
        if (insertionIndex >= _queueState.PlayList.Count)
        {
            insertionIndex = 0;
        }

        _queueState.PlayList.Insert(insertionIndex, selectedSong);
        _queueState.CurrentSongIndex = _queueState.PlayList.IndexOf(currentSong);
        _queueState.SelectedIndex = _queueState.PlayList.IndexOf(selectedSong);
        _queueState.SelectedSong = selectedSong;
    }

    /// <inheritdoc />
    public async Task ScanSelectedSongAsync()
    {
        var selectedSong = _queueState.SelectedSong;
        if (selectedSong is null || string.IsNullOrWhiteSpace(selectedSong.Path))
        {
            return;
        }

        var scanned = await _fileScanner.ScanAsync(selectedSong.Path);
        var index = _queueState.PlayList.IndexOf(selectedSong);
        if (index >= 0)
        {
            _queueState.PlayList[index] = scanned;
        }

        _queueState.SelectedSong = scanned;
    }

    private static int IndexOfSongByPath(IReadOnlyList<AudioModel> songs, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return -1;
        }

        for (var index = 0; index < songs.Count; index++)
        {
            var songPath = songs[index].Path;
            if (!string.IsNullOrWhiteSpace(songPath) &&
                songPath.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}
