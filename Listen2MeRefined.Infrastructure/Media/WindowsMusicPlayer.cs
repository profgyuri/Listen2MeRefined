using Listen2MeRefined.Infrastructure.LowLevel;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Media;

using NAudio.Wave;
using System.Collections.ObjectModel;

/// <summary>
///     Wrapper class for NAudio.
/// </summary>
public sealed class WindowsMusicPlayer : IMediaController, IPlaylistReference
{
    private bool _startSongAutomatically;
    private int _playlistIndex;
    private double _previousTimeStamp;
    private AudioModel? _currentSong;
    private AudioFileReader? _fileReader;
    private WaveOutEvent _waveOutEvent = new();
    private ObservableCollection<AudioModel> _playlist = new();
    private PlaybackState _playbackState = PlaybackState.Stopped;

    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRepository<AudioModel> _audioRepository;

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

        timedTask.Start(CurrentTimeCheck);
        keyboardHook.KeyboardPressed += KeyboardPressedEvent;
    }

    #region IMediaController
    public void PlayPause()
    {
        if (_playbackState == PlaybackState.Playing)
        {
            _waveOutEvent.Pause();
            _startSongAutomatically = false;
            _playbackState = PlaybackState.Paused;

            _logger.Debug("Playback paused");

            return;
        }
        
        _playbackState = PlaybackState.Playing;

        if (_currentSong is null && _playlist.Any())
        {
            LoadCurrentSong();
        }

        _startSongAutomatically = true;
        _waveOutEvent.Play();
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
        _playlistIndex = (_playlistIndex + 1) % _playlist!.Count;

        LoadCurrentSong();

        _logger.Debug("Jumping to the next song at index {Current} with the maximum possible index of {Maximum}...",
            _playlistIndex, _playlist.Count - 1);
    }

    public void Previous()
    {
        _playlistIndex = (_playlistIndex - 1 + _playlist!.Count) % _playlist.Count;

        LoadCurrentSong();

        _logger.Debug("Jumping to the previous song at index {Current} with the maximum possible index of {Maximum}...",
            _playlistIndex, _playlist.Count - 1);
    }

    public void Shuffle()
    {
        _logger.Information("Shuffling playlist...");

        _playlist.Shuffle();
        
        if (_currentSong is not null)
        {
            // next 2 lines will put the current song to the 1st place
            var index = _playlist.IndexOf(_currentSong);
            (_playlist[0], _playlist[index]) = (_playlist[index], _playlist[0]);
        }
        
        _playlistIndex = 0;
    }
    #endregion

    #region Helpers
    private void LoadCurrentSong()
    {
        _logger.Information("Loading audio: {Song}", _currentSong);
        _currentSong = _playlist[_playlistIndex];

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
        }
        
        _mediator.Publish(new CurrentSongNotification(_currentSong));

        ReNewWaveOutEvent();

        _previousTimeStamp = -1;

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
        if (_fileReader is null)
        {
            return;
        }

        if (Math.Abs(_fileReader.CurrentTime.TotalMilliseconds - _previousTimeStamp) < 0.1
            && _playbackState == PlaybackState.Playing
            && _previousTimeStamp >= 300)
        {
            Next();
        }

        if (_playbackState is PlaybackState.Playing)
        {
            _previousTimeStamp = _fileReader.CurrentTime.TotalMilliseconds;
        }
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