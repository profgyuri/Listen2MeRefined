using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using NAudio.Wave;
using SkiaSharp;
using Source;
using Source.Extensions;

namespace Listen2MeRefined.Infrastructure.Media;

/// <summary>
///     Wrapper class for NAudio.
/// </summary>
public sealed class WindowsMusicPlayer : 
    IMediaController<SKBitmap>, 
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
    private readonly IWaveFormDrawer<SKBitmap> _waveFormDrawer;

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

    public SKBitmap Bitmap { get; set; } = new(1, 1);

    public WindowsMusicPlayer(
        ILogger logger,
        IMediator mediator,
        TimedTask timedTask,
        IWaveFormDrawer<SKBitmap> waveFormDrawer)
    {
        _logger = logger;
        _mediator = mediator;
        _waveFormDrawer = waveFormDrawer;

        timedTask.Start(TimeSpan.FromMilliseconds(TimeCheckInterval), async () => await CurrentTimeCheck());
    }

    /// <inheritdoc />
    public void PassPlaylist(ref ObservableCollection<AudioModel> playlist)
    {
        _playlist = playlist;
    }

    #region IMediaController
    public async Task PlayPauseAsync()
    {
        if (_playbackState == PlaybackState.Playing)
        {
            PausePlayback();
            return;
        }

        await StartPlayback();
    }

    public void Stop()
    {
        _waveOutEvent.Stop();
        _playbackState = PlaybackState.Stopped;

        _logger.Debug("Playback stopped by user");

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
            _logger.Debug("Playback is stopped, because the playlist is empty!");
            Stop();
            return;
        }

        _currentSongIndex = (_currentSongIndex + 1) % _playlist!.Count;

        await LoadCurrentSong();

        _logger.Debug("Jumping to the next song at index {Current} with the maximum possible index of {Maximum}...",
            _currentSongIndex, _playlist.Count - 1);
    }

    public async Task PreviousAsync()
    {
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
        _logger.Information("Shuffling playlist...");

        _playlist.Shuffle();

        var index = _playlist.IndexOf(_currentSong);
        _currentSongIndex = 0;

        if (index > -1)
        {
            (_playlist[index], _playlist[0]) = (_playlist[0], _playlist[index]);
        }
        else
        {
            await LoadCurrentSong();
        }
    }
    #endregion

    #region Helpers
    private async Task LoadCurrentSong()
    {
        _currentSong = _playlist[_currentSongIndex];
        _logger.Information("Loading audio: {Song}", _currentSong);

        if (!File.Exists(_currentSong.Path))
        {
            _logger.Information("Skipping a song, that does not exist anymore at path: " + _currentSong.Path);
            _playlist.Remove(_currentSong);
            await NextAsync();
            return;
        }

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

        Bitmap = await _waveFormDrawer.WaveFormAsync(_currentSong.Path);
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
        _logger.Debug("Renewing waveout event...");

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
        if (ShouldSkipTimeCheck())
        {
            return;
        }

        if (ShouldSkipToNext())
        {
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