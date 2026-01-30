namespace Listen2MeRefined.Infrastructure.Media;
using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using NAudio.Wave;

/// <summary>
/// Wrapper class for NAudio.
/// </summary>
public sealed class WindowsMusicPlayer : 
    IMediaController, 
    IPlaylistReference,
    INotificationHandler<AudioOutputDeviceChangedNotification>
{
    private bool _startSongAutomatically;
    private int _currentSongIndex;
    private int _outputDeviceIndex = -1;
    private double _previousTimeStamp;
    private double _unpausedFor;
    private AudioModel? _currentSong;
    private WaveStream? _fileReader;
    private WaveOutEvent _waveOutEvent = new();
    private ObservableCollection<AudioModel> _playlist = new();
    private PlaybackState _playbackState = PlaybackState.Stopped;

    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    private const int TimeCheckInterval = 500;

    public double CurrentTime
    {
        get => _fileReader?.CurrentTime.TotalMilliseconds ?? 0;
        set
        {
            if (_fileReader == null)
            {
                return;
            }

            _fileReader.CurrentTime = TimeSpan.FromMilliseconds(value);
        }
    }

    public float Volume
    {
        get => _waveOutEvent.Volume;
        set => _waveOutEvent.Volume = value;
    }

    public WindowsMusicPlayer(
        ILogger logger,
        IMediator mediator,
        TimedTask timedTask)
    {
        _logger = logger;
        _mediator = mediator;

        timedTask.Start(TimeSpan.FromMilliseconds(TimeCheckInterval), async () => await CurrentTimeCheck());
        _logger.Information("[WindowsMusicPlayer] initialized");
    }

    /// <inheritdoc />
    public void PassPlaylist(ref ObservableCollection<AudioModel> playlist)
    {
        _playlist = playlist;
    }

    public async Task PlayPauseAsync()
    {
        _logger.Information("[WindowsMusicPlayer] Toggling playback state...");
        if (_currentSong is null)
        {
            _logger.Information("[WindowsMusicPlayer] No song is loaded, do nothing...");
            return;
        }

        if (_playbackState == PlaybackState.Playing)
        {
            _logger.Information("[WindowsMusicPlayer] Pausing playback");
            PausePlayback();
            return;
        }

        _logger.Information("[WindowsMusicPlayer] Starting playback");
        await StartPlayback();
    }

    public void Stop()
    {
        _waveOutEvent.Stop();
        _playbackState = PlaybackState.Stopped;

        _logger.Information("[WindowsMusicPlayer] Playback stopped by user");

        if (_fileReader is not null)
        {
            _fileReader.CurrentTime = TimeSpan.FromSeconds(0);
        }

        _startSongAutomatically = false;
    }

    public async Task NextAsync()
    {
        if (!_playlist.Any())
        {
            _logger.Information("[WindowsMusicPlayer] Playback is stopped, because the playlist is empty!");
            Stop();
            return;
        }

        _currentSongIndex = (_currentSongIndex + 1) % _playlist!.Count;

        _logger.Information("[WindowsMusicPlayer] Jumping to next song...");
        await LoadCurrentSong();
    }

    public async Task PreviousAsync()
    {
        if (!_playlist.Any())
        {
            _logger.Information("[WindowsMusicPlayer] Cannot go to the previous song, because the playlist is empty!");
            return;
        }

        _currentSongIndex = (_currentSongIndex - 1 + _playlist!.Count) % _playlist.Count;

        _logger.Information("[WindowsMusicPlayer] Jumping to previous song...");
        await LoadCurrentSong();
    }

    public async Task JumpToIndexAsync(int index)
    {
        _currentSongIndex = index;
        _logger.Information("[WindowsMusicPlayer] Jumping to song at index {Index}...", index);
        await LoadCurrentSong();
    }

    public async Task Shuffle()
    {
        if (!_playlist.Any())
        {
            _logger.Information("[WindowsMusicPlayer] Cannot shuffle an empty playlist!");
            return;
        }

        _logger.Information("[WindowsMusicPlayer] Shuffling playlist...");

        _playlist.Shuffle();
        _logger.Information("[WindowsMusicPlayer] Playlist shuffled.");

        var index = _playlist.IndexOf(_currentSong);
        _currentSongIndex = 0;

        if (index > -1)
        {
            _logger.Information("[WindowsMusicPlayer] Moving current song to the top of the shuffled playlist...");
            (_playlist[index], _playlist[0]) = (_playlist[0], _playlist[index]);
        }
        else
        {
            _logger.Information("[WindowsMusicPlayer] Current song not found in playlist, loading the first song...");
            await LoadCurrentSong();
        }
    }
    private async Task LoadCurrentSong()
    {
        _currentSong = _playlist[_currentSongIndex];
        _logger.Information("[WindowsMusicPlayer] Loading audio: {Song}", _currentSong);

        if (!File.Exists(_currentSong.Path))
        {
            _logger.Information("[WindowsMusicPlayer] Skipping a song, that does not exist anymore at path: " + _currentSong.Path);
            _playlist.Remove(_currentSong);
            await NextAsync();
            return;
        }

        _logger.Verbose("[WindowsMusicPlayer] Determining the type of audio file reader");
        _fileReader = _currentSong.Path.EndsWith(".wav") ? 
            new WaveFileReader(_currentSong.Path) : 
            new AudioFileReader(_currentSong.Path);

        if (_fileReader.WaveFormat.Encoding is WaveFormatEncoding.Extensible)
        {
            //just skip this, as I have no clue how to handle or convert this type
            _logger.Information("[WindowsMusicPlayer] Unsupported (extensible) .wav file is being skipped: {Song}", _currentSong.Path);
            await NextAsync();
            return;
        }

        _logger.Verbose("[WindowsMusicPlayer] Publishing current song changed notification...");
        await _mediator.Publish(new CurrentSongNotification(_currentSong));

        ReNewWaveOutEvent();

        _previousTimeStamp = -1;
        _unpausedFor = 0;

        if (_startSongAutomatically)
        {
            _logger.Information("[WindowsMusicPlayer] Starting playback of the loaded song automatically...");
            _waveOutEvent!.Play();
        }
    }

    private void ReNewWaveOutEvent()
    {
        _logger.Information("[WindowsMusicPlayer] Renewing waveout event...");

        try
        {
            _waveOutEvent.Dispose();
            _waveOutEvent = new WaveOutEvent
            {
                DeviceNumber = _outputDeviceIndex
            };
            _waveOutEvent.Init(_fileReader);
        }
        catch (Exception e)
        {
            _logger.Error(e, "[WindowsMusicPlayer] Waveout event failed to load!");
        }
    }

    private async Task CurrentTimeCheck()
    {
        if (ShouldSkipTimeCheck())
        {
            _logger.Debug("[WindowsMusicPlayer] Skipping time check...");
            return;
        }

        if (ShouldSkipToNext())
        {
            _logger.Information("[WindowsMusicPlayer] Current song reached its end, skipping to the next song...");
            await NextAsync();
        }

        _previousTimeStamp = _fileReader!.CurrentTime.TotalMilliseconds;
        _unpausedFor += TimeCheckInterval;
    }

    /// <summary>
    /// Checks if any song is being played currently.
    /// </summary>
    /// <returns>True if a song is being played, false otherwise.</returns>
    private bool ShouldSkipTimeCheck()
    {
        return _fileReader is null || _playbackState is not PlaybackState.Playing;
    }

    /// <summary>
    /// Determines if the remainder of the current song should be skipped.
    /// </summary>
    /// <returns>True if the song reached it's end, false otherwise.</returns>
    private bool ShouldSkipToNext()
    {
        return Math.Abs(_fileReader!.CurrentTime.TotalMilliseconds - _previousTimeStamp) < 0.1
               && _previousTimeStamp >= TimeCheckInterval
               && _unpausedFor > TimeCheckInterval
               && _fileReader.CurrentTime.TotalMilliseconds > _fileReader.TotalTime.TotalMilliseconds - 1000;
    }

    private async Task StartPlayback()
    {
        if (!_playlist.Any())
        {
            _logger.Warning("[WindowsMusicPlayer] Cannot start playback, because the playlist is empty!");
            return;
        }

        _playbackState = PlaybackState.Playing;
        _unpausedFor = 0;

        if (_currentSong is null)
        {
            _logger.Information("[WindowsMusicPlayer] No song is loaded, loading the current song...");
            await LoadCurrentSong();
        }

        _startSongAutomatically = true;
        _waveOutEvent.Play();
        _logger.Information("[WindowsMusicPlayer] Playback started");
    }

    private void PausePlayback()
    {
        _logger.Information("[WindowsMusicPlayer] Pausing playback...");
        _waveOutEvent.Pause();
        _startSongAutomatically = false;
        _playbackState = PlaybackState.Paused;

        _logger.Information("[WindowsMusicPlayer] Playback paused");
    }

    /// <inheritdoc />
    public async Task Handle(
        AudioOutputDeviceChangedNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.Information("[WindowsMusicPlayer] Received audio output device changed notification: {DeviceName}", notification.Device.Name);
        // Avoid changing to the same device
        if (notification.Device.Index == _outputDeviceIndex)
        {
            _logger.Information("[WindowsMusicPlayer] The selected audio output device is already in use. No changes made.");
            return;
        }

        _logger.Information("[WindowsMusicPlayer] Changing audio output device to {DeviceName}", notification.Device.Name);
        _outputDeviceIndex = notification.Device.Index;

        if (_fileReader is null)
        {
            _logger.Information("[WindowsMusicPlayer] FileReader is not yet initialized, skipping device change...");
            return;
        }
        
        var timeStamp = _fileReader.CurrentTime;

        var isPlaying = _playbackState == PlaybackState.Playing;
        
        ReNewWaveOutEvent();
        Stop();

        _fileReader.CurrentTime = timeStamp;

        if (isPlaying)
        {
            _logger.Information("[WindowsMusicPlayer] Resuming playback after device change...");
            await PlayPauseAsync();
        }

        _logger.Information("[WindowsMusicPlayer] Audio output device changed successfully to {DeviceName}", notification.Device.Name);
    }
}