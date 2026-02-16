namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class PlaybackQueueService : IPlaybackQueueService
{
    private readonly IPlaylist _playlist;

    public PlaybackQueueService(IPlaylist playlist)
    {
        _playlist = playlist;
    }
    
    public AudioModel? GetCurrentTrack()
    {
        if (!_playlist.Any())
        {
            return null;
        }

        NormalizeCurrentIndex();
        
        var track = _playlist[_playlist.CurrentIndex];
        
        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? GetNextTrack()
    {
        if (!_playlist.Any())
        {
            return null;
        }

        _playlist.CurrentIndex = (_playlist.CurrentIndex + 1) % _playlist.Count;
        var track = _playlist[_playlist.CurrentIndex];
        
        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? GetPreviousTrack()
    {
        if (!_playlist.Any())
        {
            return null;
        }

        _playlist.CurrentIndex = (_playlist.CurrentIndex - 1 + _playlist.Count) % _playlist.Count;
        var track = _playlist[_playlist.CurrentIndex];

        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? GetTrackAtIndex(int index)
    {
        if (!_playlist.Any())
        {
            return null;
        }

        var normalizedIndex = index % _playlist.Count;
        if (normalizedIndex < 0)
        {
            normalizedIndex += _playlist.Count;
        }

        _playlist.CurrentIndex = normalizedIndex;
        var track = _playlist[_playlist.CurrentIndex];

        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? Shuffle(AudioModel? currentTrack)
    {
        if (!_playlist.Any())
        {
            return null;
        }

        _playlist.Shuffle();

        var currentIndex = _playlist.IndexOf(currentTrack);
        if (currentIndex > 0)
        {
            _playlist.Move(currentIndex, 0);
        }

        _playlist.CurrentIndex = 0;
        return _playlist[0];
    }

    public bool RemoveTrack(AudioModel track)
    {
        var removeIndex = _playlist.IndexOf(track);
        if (removeIndex < 0)
        {
            return false;
        }

        var removed = _playlist.Remove(track);
        if (!removed)
        {
            return false;
        }

        if (_playlist.Count == 0)
        {
            _playlist.CurrentIndex = 0;
            return true;
        }

        if (removeIndex < _playlist.CurrentIndex)
        {
            _playlist.CurrentIndex--;
        }
        else if (_playlist.CurrentIndex >= _playlist.Count)
        {
            _playlist.CurrentIndex = 0;
        }

        return true;
    }
    
    /// <summary>
    ///     Removes the track from the playlist if the file is not found and moves the current index accordingly.
    /// </summary>
    /// <param name="audio">The track to remove.</param>
    /// <returns>True if the track was removed, false otherwise.</returns>
    private bool RemoveIfInvalid(AudioModel audio)
    {
        if (!_playlist.Any())
        {
            _playlist.CurrentIndex = 0;
            return false;
        }

        if (File.Exists(audio.Path))
        {
            return false;
        }

        var index = _playlist.IndexOf(audio);
        _playlist.Remove(audio);
        if (index < _playlist.CurrentIndex)
        {
            _playlist.CurrentIndex--;
        }

        if (!_playlist.Any())
        {
            _playlist.CurrentIndex = 0;
            return false;
        }

        NormalizeCurrentIndex();
        return true;
    }

    private void NormalizeCurrentIndex()
    {
        if (_playlist.Count == 0 || _playlist.CurrentIndex < 0)
        {
            _playlist.CurrentIndex = 0;
            return;
        }

        if (_playlist.CurrentIndex >= _playlist.Count)
        {
            _playlist.CurrentIndex = _playlist.Count - 1;
        }
    }
}