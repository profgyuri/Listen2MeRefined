using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Notifications;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

/// <summary>
/// Wrapper class for NAudio.
/// </summary>
public sealed partial class NAudioMusicPlayer :
    IMusicPlayerController,
    INotificationHandler<AudioOutputDeviceChangedNotification>
{
    private bool _startSongAutomatically;
    private int _outputDeviceIndex = -1;
    private AudioModel? _currentSong;
    private NAudio.Wave.WaveStream? _fileReader;
    private PlayerState _state = PlayerState.Stopped;

    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IPlaybackQueueService _playbackQueueService;
    private readonly ITrackLoader _trackLoader;
    private readonly IPlaybackOutput _playbackOutput;
    private readonly IPlaybackProgressMonitor _playbackProgressMonitor;

    private const int TimeCheckInterval = 500;

    /// <summary>
    /// Gets or sets the current playback position in milliseconds.
    /// </summary>
    public double CurrentTime
    {
        get => _fileReader?.CurrentTime.TotalMilliseconds ?? 0;
        set
        {
            if (_fileReader is not null)
            {
                _fileReader.CurrentTime = TimeSpan.FromMilliseconds(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the playback output volume.
    /// </summary>
    public float Volume
    {
        get => _playbackOutput.Volume;
        set => _playbackOutput.Volume = value;
    }

    /// <summary>
    /// Initializes a new player orchestrator with the required playback collaborators.
    /// </summary>
    public NAudioMusicPlayer(
        ILogger logger,
        IMediator mediator,
        TimedTask timedTask,
        IPlaybackQueueService playbackQueueService,
        ITrackLoader trackLoader,
        IPlaybackOutput playbackOutput,
        IPlaybackProgressMonitor playbackProgressMonitor)
    {
        _logger = logger;
        _mediator = mediator;
        _playbackQueueService = playbackQueueService;
        _trackLoader = trackLoader;
        _playbackOutput = playbackOutput;
        _playbackProgressMonitor = playbackProgressMonitor;

        timedTask.Start(TimeSpan.FromMilliseconds(TimeCheckInterval), () => CheckPlaybackProgressAsync().GetAwaiter().GetResult());
        _logger.Debug("[NAudioMMusicPlayer] initialized");
    }

    /// <summary>
    /// Toggles playback between play and pause for the current track.
    /// </summary>
    public async Task PlayPauseAsync()
    {
        if (_currentSong is null)
        {
            await StartPlaybackAsync();
            return;
        }

        if (_state == PlayerState.Playing)
        {
            PausePlayback();
            return;
        }

        await StartPlaybackAsync();
    }

    /// <summary>
    /// Stops playback and seeks to the beginning of the current track.
    /// </summary>
    public void Stop()
    {
        _playbackOutput.Stop();
        if (_fileReader is not null)
        {
            _fileReader.CurrentTime = TimeSpan.Zero;
        }

        _startSongAutomatically = false;
        SetState(PlayerState.Stopped);
        _playbackProgressMonitor.Reset();
        _logger.Debug("[NAudioMMusicPlayer] Playback stopped by user");
    }

    /// <summary>
    /// Advances to the next track in the playback queue.
    /// </summary>
    public async Task NextAsync()
    {
        var nextTrack = _playbackQueueService.GetNextTrack();
        if (nextTrack is null)
        {
            _logger.Information("[NAudioMMusicPlayer] Playback is stopped, because the playlist is empty!");
            Stop();
            return;
        }

        await LoadSongAsync(nextTrack);
    }

    /// <summary>
    /// Moves to the previous track in the playback queue.
    /// </summary>
    public async Task PreviousAsync()
    {
        var previousTrack = _playbackQueueService.GetPreviousTrack();
        if (previousTrack is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot go to the previous song, because the playlist is empty!");
            return;
        }

        await LoadSongAsync(previousTrack);
    }

    /// <summary>
    /// Jumps playback to the track at the provided queue index.
    /// </summary>
    /// <param name="index">The target track index.</param>
    public async Task JumpToIndexAsync(int index)
    {
        var track = _playbackQueueService.GetTrackAtIndex(index);
        if (track is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot jump to song at index {Index}, because the playlist is empty!", index);
            return;
        }

        if (_state == PlayerState.Playing &&
            _currentSong is not null &&
            string.Equals(track.Path, _currentSong.Path, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Debug("[NAudioMMusicPlayer] Ignoring jump to already playing track at index {Index}", index);
            return;
        }

        await LoadSongAsync(track);
    }

    /// <summary>
    /// Shuffles the queue while keeping the current track consistent when available.
    /// </summary>
    public async Task Shuffle()
    {
        var shuffledCurrentTrack = _playbackQueueService.Shuffle(_currentSong);
        if (shuffledCurrentTrack is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot shuffle an empty playlist!");
            return;
        }

        await _mediator.Publish(new PlaylistShuffledNotification());

        if (_currentSong is null)
        {
            await LoadSongAsync(shuffledCurrentTrack);
        }
    }

    internal async Task CheckPlaybackProgressAsync()
    {
        if (_fileReader is null)
        {
            return;
        }

        if (_playbackProgressMonitor.ShouldAdvance(_fileReader.CurrentTime, _fileReader.TotalTime, _state == PlayerState.Playing))
        {
            _logger.Debug("[NAudioMMusicPlayer] Current song reached its end, skipping to the next song...");
            await NextAsync();
        }
    }

    /// <summary>
    /// Reconfigures audio output when the selected audio device changes.
    /// </summary>
    /// <param name="notification">The device change notification.</param>
    /// <param name="cancellationToken">Token that can cancel notification processing.</param>
    public async Task Handle(AudioOutputDeviceChangedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Device.Index == _outputDeviceIndex)
        {
            return;
        }

        _outputDeviceIndex = notification.Device.Index;
        await ReconfigureOutputAsync(_state == PlayerState.Playing, preservePosition: true);
    }

    private async Task StartPlaybackAsync()
    {
        var currentTrack = _playbackQueueService.GetCurrentTrack();
        if (currentTrack is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot start playback, because the playlist is empty!");
            return;
        }

        if (_currentSong is null)
        {
            _startSongAutomatically = true;
            await LoadSongAsync(currentTrack);
            return;
        }

        _startSongAutomatically = true;
        _playbackOutput.Play();
        SetState(PlayerState.Playing);
    }

    private void PausePlayback()
    {
        _playbackOutput.Pause();
        _startSongAutomatically = false;
        SetState(PlayerState.Paused);
    }

    private async Task LoadCurrentSongAsync()
    {
        var currentTrack = _playbackQueueService.GetCurrentTrack();
        if (currentTrack is null)
        {
            Stop();
            return;
        }

        await LoadSongAsync(currentTrack);
    }

    private async Task LoadSongAsync(AudioModel song)
    {
        var wasPlayingBeforeSwitch = _state == PlayerState.Playing;
        var shouldResumeAfterLoad = wasPlayingBeforeSwitch || _startSongAutomatically;
        var isTrackSwitch = _currentSong is null ||
                            !string.Equals(_currentSong.Path, song.Path, StringComparison.OrdinalIgnoreCase);

        if (isTrackSwitch && wasPlayingBeforeSwitch)
        {
            _playbackOutput.Pause();
            SetState(PlayerState.Paused);
        }

        _currentSong = song;

        var loadResult = _trackLoader.Load(song);
        if (!loadResult.IsSuccess)
        {
            await HandleUnplayableTrackAsync(song, loadResult);
            return;
        }

        _fileReader?.Dispose();
        _fileReader = loadResult.Reader;

        await _mediator.Publish(new CurrentSongNotification(_currentSong));

        _playbackProgressMonitor.Reset();

        var reconfigured = await ReconfigureOutputAsync(shouldResumeAfterLoad, preservePosition: false);
        if (reconfigured && shouldResumeAfterLoad)
        {
            SetState(PlayerState.Playing);
        }
    }

    private async Task<bool> ReconfigureOutputAsync(bool resumePlayback, bool preservePosition)
    {
        if (_fileReader is null)
        {
            return false;
        }

        var timeStamp = _fileReader.CurrentTime;
        var result = _playbackOutput.Reinitialize(_fileReader, _outputDeviceIndex);
        if (!result.IsSuccess)
        {
            _logger.Warning(result.Exception, "[NAudioMMusicPlayer] Failed to reconfigure audio output: {Context}", result.Context);
            if (!result.PreservedPreviousOutput)
            {
                _startSongAutomatically = false;
                SetState(PlayerState.Stopped);
            }

            return false;
        }

        if (preservePosition)
        {
            _fileReader.CurrentTime = timeStamp;
        }

        if (resumePlayback)
        {
            _playbackOutput.Play();
            SetState(PlayerState.Playing);
            _startSongAutomatically = true;
        }
        else if (_state == PlayerState.Paused)
        {
            _playbackOutput.Pause();
        }

        return true;
    }

    private async Task HandleUnplayableTrackAsync(AudioModel track, TrackLoadResult result)
    {
        _logger.Warning("[NAudioMMusicPlayer] Skipping song {Path}. Status: {Status}. Reason: {Reason}", track.Path, result.Status, result.Reason);
        _playbackQueueService.RemoveTrack(track);
        await LoadCurrentSongAsync();
    }

    private void SetState(PlayerState newState)
    {
        _mediator.Publish(new PlayerStateChangedNotification(newState));
        _state = newState;
    }
}
