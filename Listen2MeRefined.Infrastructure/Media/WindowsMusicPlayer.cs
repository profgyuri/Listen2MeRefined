namespace Listen2MeRefined.Infrastructure.Media;
using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using NAudio.Wave;
using SkiaSharp;

/// <summary>
///     Wrapper class for NAudio.
/// </summary>
public sealed class WindowsMusicPlayer : 
    IMediaController,
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
    private readonly IPlaylistStore _playlistStore;

    private const int TimeCheckInterval = 500;
    private readonly SemaphoreSlim _tickGate = new(1, 1);

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
        IPlaylistStore playlistStore,
        TimedTask timedTask)
    {
        _logger = logger;
        _mediator = mediator;
        _playlistStore = playlistStore;

        timedTask.Start(TimeSpan.FromMilliseconds(TimeCheckInterval), async () => await CurrentTimeCheck());
    }

    #region IMediaController
    public async Task PlayPauseAsync()
    {
        if (_currentSong is null)
        {
            return;
        }

        if (_playbackState == PlaybackState.Playing)
        {
            _logger.Verbose("Pausing playback");
            PausePlayback();
            return;
        }

        _logger.Verbose("Starting playback");
        await StartPlayback();
    }

    public void Stop()
    {
        _waveOutEvent.Stop();
        _playbackState = PlaybackState.Stopped;

        _logger.Verbose("Playback stopped by user");

        if (_fileReader is not null)
        {
            _fileReader.CurrentTime = TimeSpan.FromSeconds(0);
        }

        _startSongAutomatically = false;
    }

    public async Task NextAsync()
    {
        var list = _playlistStore.Snapshot();
        if (list.Count == 0)
        {
            _logger.Verbose("Playback is stopped, because the playlist is empty!");
            Stop();
            return;
        }

        _currentSongIndex = (_currentSongIndex + 1) % list.Count;
        await LoadCurrentSong(list);
    }

    public async Task PreviousAsync()
    {
        if (!_playlist.Any())
        {
            return;
        }

        _currentSongIndex = (_currentSongIndex - 1 + _playlist!.Count) % _playlist.Count;

        await LoadCurrentSong();

        _logger.Debug("Jumping to the previous song at index {Current} with the maximum possible index of {Maximum}...",
            _currentSongIndex, _playlist.Count - 1);
    }

    public async Task JumpToIndexAsync(int index)
    {
        _currentSongIndex = index;
        await LoadCurrentSong();
    }

    public async Task Shuffle()
    {
        var list = _playlistStore.Snapshot();
        if (list.Count == 0) return;

        var currentPath = _currentSong?.Path;
        _logger.Information("Shuffling playlist...");

        _playlistStore.Shuffle(keepFirstByPath: true, firstPath: currentPath);

        _currentSongIndex = 0;
        await LoadCurrentSong(_playlistStore.Snapshot());
    }
    #endregion

    #region Helpers
    private async Task LoadCurrentSong(IReadOnlyList<AudioModel>? list = null)
    {
        list ??= _playlistStore.Snapshot();
        if (list.Count == 0)
        {
            Stop();
            return;
        }
        
        _currentSongIndex = Math.Clamp(_currentSongIndex, 0, list.Count - 1);
        _currentSong = list[_currentSongIndex];
        _logger.Information("Loading audio: {Song}", _currentSong);

        if (!File.Exists(_currentSong.Path))
        {
            _logger.Debug("Removing missing file from playlist: {Path}", _currentSong.Path);
            if (_currentSong.Path is not null)
                _playlistStore.RemoveByPath(_currentSong.Path);

            await NextAsync();
            return;
        }

        _logger.Verbose("Determining the type of audio file reader");
        _fileReader = _currentSong.Path.EndsWith(".wav") ? 
            new WaveFileReader(_currentSong.Path) : 
            new AudioFileReader(_currentSong.Path);

        if (_fileReader.WaveFormat.Encoding is WaveFormatEncoding.Extensible)
        {
            //just skip this, as I have no clue how to handle or convert this type
            _logger.Debug("Unsupported (extensible) .wav file is being skipped: {Song}", _currentSong.Path);
            await NextAsync();
            return;
        }

        _logger.Verbose("Publishing notification for the current song has changed");
        await _mediator.Publish(new CurrentSongNotification(_currentSong));

        ReNewWaveOutEvent();

        _previousTimeStamp = -1;
        _unpausedFor = 0;

        if (_startSongAutomatically)
        {
            _waveOutEvent!.Play();
        }
    }

    private void ReNewWaveOutEvent()
    {
        _logger.Information("Renewing waveout event...");

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
            _logger.Error(e, "Waveout event failed to load!");
        }
    }

    private async Task CurrentTimeCheck()
    {
        if (!await _tickGate.WaitAsync(0))
            return;

        try
        {
            if (ShouldSkipTimeCheck())
                return;

            if (ShouldSkipToNext())
                await NextAsync();

            _previousTimeStamp = _fileReader!.CurrentTime.TotalMilliseconds;
            _unpausedFor += TimeCheckInterval;
        }
        finally
        {
            _tickGate.Release();
        }
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
    /// <returns>True if the song reached its end, false otherwise.</returns>
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
            return;
        }

        _playbackState = PlaybackState.Playing;
        _unpausedFor = 0;

        if (_currentSong is null)
        {
            await LoadCurrentSong();
        }

        _startSongAutomatically = true;
        _waveOutEvent.Play();
        _logger.Debug("Playback started");
    }

    private void PausePlayback()
    {
        _waveOutEvent.Pause();
        _startSongAutomatically = false;
        _playbackState = PlaybackState.Paused;

        _logger.Debug("Playback paused");
    }
    #endregion

    #region Implementation of INotificationHandler<in AudioOutputDeviceChangedNotification>
    /// <inheritdoc />
    public async Task Handle(
        AudioOutputDeviceChangedNotification notification,
        CancellationToken cancellationToken)
    {
        // Avoid changing to the same device
        if (notification.Device.Index == _outputDeviceIndex)
        {
            return;
        }

        _logger.Information("Changing audio output device to {DeviceName}", notification.Device.Name);
        _outputDeviceIndex = notification.Device.Index;

        if (_fileReader is null)
        {
            return;
        }
        
        var timeStamp = _fileReader.CurrentTime;

        var isPlaying = _playbackState == PlaybackState.Playing;
        
        ReNewWaveOutEvent();
        Stop();

        _fileReader.CurrentTime = timeStamp;

        if (isPlaying)
        {
            await PlayPauseAsync();
        }
    }
    #endregion
}