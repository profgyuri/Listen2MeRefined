using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;

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

        var selectedSongIndex = _queueState.PlayList.IndexOf(selectedSong);
        var newIndex = _queueState.CurrentSongIndex + 1;
        if (newIndex >= _queueState.PlayList.Count)
        {
            newIndex = 0;
        }

        _queueState.PlayList.Move(selectedSongIndex, newIndex);
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
}
