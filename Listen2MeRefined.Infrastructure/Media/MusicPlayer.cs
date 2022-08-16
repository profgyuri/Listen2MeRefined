using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Media;

using NAudio.Wave;
using System.Collections.ObjectModel;

/// <summary>
///     Wrapper class for NAudio.
/// </summary>
public sealed class MusicPlayer : IMediaController, IPlaylistReference
{
    private bool _startSongAutomatically;
    private int _playlistIndex;
    private double _previousTimeStamp = -1;
    private AudioModel? _currentSong;
    private AudioFileReader? _fileReader;
    private WaveOutEvent _waveOutEvent = new();
    private ObservableCollection<AudioModel> _playlist = new();
    private PlaybackStoppedFor _stoppedFor = PlaybackStoppedFor.EndOfTrack;
    private PlaybackState _playbackState = PlaybackState.Stopped;

    private readonly ILogger _logger;
    private readonly IMediator _mediator;

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

    public MusicPlayer(ILogger logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;

        _waveOutEvent.PlaybackStopped += PlaybackStoppedEvent;
    }

    #region IMediaController
    public void PlayPause()
    {
        if (_playbackState == PlaybackState.Playing)
        {
            _waveOutEvent?.Pause();
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
        _waveOutEvent?.Play();
        _stoppedFor = PlaybackStoppedFor.EndOfTrack;
    }

    public void Stop()
    {
        _waveOutEvent?.Stop();
        _stoppedFor = PlaybackStoppedFor.UserInput;
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
            _logger.Error("{song} was null", nameof(_currentSong));
        }
        catch (FileNotFoundException)
        {
            _logger.Warning("File was not found at: {Path} - Trying to remove entry from database...", _currentSong.Path);
            //todo: remove missing file from database
            throw new NotImplementedException("Deletion should be implemented!");
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
            _waveOutEvent?.Dispose();
            _waveOutEvent = new WaveOutEvent();
            _waveOutEvent.Init(_fileReader);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Waveout event failed to load!");
        }
    }
    #endregion

    #region Event handlers
    private void PlaybackStoppedEvent(object? sender, StoppedEventArgs e)
    {
        if (_stoppedFor == PlaybackStoppedFor.EndOfTrack)
        {
            Next();
        }
    }
    #endregion

    /// <inheritdoc />
    public void PassPlaylist(ref ObservableCollection<AudioModel> playlist)
    {
        _playlist = playlist;
    }
}