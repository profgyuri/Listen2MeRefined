using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Source;
using Source.Extensions;
using Source.KeyboardHook;
using NAudio.Wave;
using System.Collections.ObjectModel;

namespace Listen2MeRefined.Infrastructure.Media;

/// <summary>
///     Wrapper class for NAudio.
/// </summary>
public sealed class WindowsMusicPlayer : IMediaController, IPlaylistReference
{
    private bool _startSongAutomatically;
    private int _currentSongIndex;
    private double _previousTimeStamp;
    private double _unpausedFor;
    private AudioModel? _currentSong;
    private AudioFileReader? _fileReader;
    private WaveOutEvent _waveOutEvent = new();
    private ObservableCollection<AudioModel> _playlist = new();
    private PlaybackState _playbackState = PlaybackState.Stopped;

    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRepository<AudioModel> _audioRepository;
    
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

    public WindowsMusicPlayer(ILogger logger, IMediator mediator, TimedTask timedTask,
        KeyboardHook keyboardHook, IRepository<AudioModel> audioRepository)
    {
        _logger = logger;
        _mediator = mediator;
        _audioRepository = audioRepository;

        timedTask.Start(TimeSpan.FromMilliseconds(TimeCheckInterval), CurrentTimeCheck);
        keyboardHook.KeyboardPressed += KeyboardPressedEvent;
    }

    #region IMediaController
    public void PlayPause()
    {
        if (_playbackState == PlaybackState.Playing)
        {
            PausePlayback();
            return;
        }

        StartPlayback();
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

    public void Next()
    {
        _currentSongIndex = (_currentSongIndex + 1) % _playlist!.Count;

        LoadCurrentSong();

        _logger.Debug("Jumping to the next song at index {Current} with the maximum possible index of {Maximum}...",
            _currentSongIndex, _playlist.Count - 1);
    }

    public void Previous()
    {
        _currentSongIndex = (_currentSongIndex - 1 + _playlist!.Count) % _playlist.Count;

        LoadCurrentSong();

        _logger.Debug("Jumping to the previous song at index {Current} with the maximum possible index of {Maximum}...",
            _currentSongIndex, _playlist.Count - 1);
    }
    
    public void JumpToIndex(int index)
    {
        _currentSongIndex = index;
        LoadCurrentSong();
    }

    public void Shuffle()
    {
        _logger.Information("Shuffling playlist...");

        _playlist.Shuffle();
        
        if (_currentSong is not null)
        {
            (_playlist[0], _playlist[_currentSongIndex]) = 
                (_playlist[_currentSongIndex], _playlist[0]);
        }
        
        _currentSongIndex = 0;
    }
    #endregion

    #region Helpers
    private void LoadCurrentSong()
    {
        _logger.Information("Loading audio: {Song}", _currentSong);
        _currentSong = _playlist[_currentSongIndex];

        try
        {
            _fileReader = new AudioFileReader(_currentSong.Path);
        }
        catch (NullReferenceException)
        {
            _logger.Error("{Song} was null", nameof(_currentSong));
            Next();
        }
        catch (FileNotFoundException)
        {
            _logger.Warning("File was not found at: {Path} - Trying to remove entry from database...", _currentSong.Path);
            _audioRepository.Delete(_currentSong);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An unexpected error occured!");
            throw;
        }
        
        _mediator.Publish(new CurrentSongNotification(_currentSong));

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
            _waveOutEvent = new WaveOutEvent();
            _waveOutEvent.Init(_fileReader);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Waveout event failed to load!");
        }
    }

    private void CurrentTimeCheck()
    {
        if (_fileReader is null
            || _playbackState is not PlaybackState.Playing)
        {
            return;
        }

        if (Math.Abs(_fileReader.CurrentTime.TotalMilliseconds - _previousTimeStamp) < 0.1
            && _previousTimeStamp >= TimeCheckInterval
            && _unpausedFor > TimeCheckInterval)
        {
            Next();
        }

        _previousTimeStamp = _fileReader.CurrentTime.TotalMilliseconds;
        _unpausedFor += TimeCheckInterval;
    }
    
    private void StartPlayback()
    {
        _playbackState = PlaybackState.Playing;
        _unpausedFor = 0;

        if (_currentSong is null && _playlist.Any())
        {
            LoadCurrentSong();
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

    #region Event handlers
    private void KeyboardPressedEvent(object? sender, KeyboardHookEventArgs e)
    {
        if (e.KeyboardState == KeyboardState.KeyUp)
        {
            return;
        }
        
        switch (e.KeyboardData.Key)
        {
            case ConsoleKey.MediaPlay:
                PlayPause();
                break;
            case ConsoleKey.MediaStop:
                Stop();
                break;
            case ConsoleKey.MediaNext:
                Next();
                break;
            case ConsoleKey.MediaPrevious:
                Previous();
                break;
        }
    }
    #endregion

    /// <inheritdoc />
    public void PassPlaylist(ref ObservableCollection<AudioModel> playlist)
    {
        _playlist = playlist;
    }
}