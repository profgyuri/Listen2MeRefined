using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class PlaybackQueueService : IPlaybackQueueService
{
    private readonly IPlaylistQueue _playlistQueue;

    public PlaybackQueueService(IPlaylistQueue playlistQueue)
    {
        _playlistQueue = playlistQueue;
    }
    
    public AudioModel? GetCurrentTrack()
    {
        if (!_playlistQueue.Any())
        {
            return null;
        }

        NormalizeCurrentIndex();
        
        var track = _playlistQueue[_playlistQueue.CurrentIndex];
        
        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? GetNextTrack()
    {
        if (!_playlistQueue.Any())
        {
            return null;
        }

        _playlistQueue.CurrentIndex = (_playlistQueue.CurrentIndex + 1) % _playlistQueue.Count;
        var track = _playlistQueue[_playlistQueue.CurrentIndex];
        
        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? GetPreviousTrack()
    {
        if (!_playlistQueue.Any())
        {
            return null;
        }

        _playlistQueue.CurrentIndex = (_playlistQueue.CurrentIndex - 1 + _playlistQueue.Count) % _playlistQueue.Count;
        var track = _playlistQueue[_playlistQueue.CurrentIndex];

        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? GetTrackAtIndex(int index)
    {
        if (!_playlistQueue.Any())
        {
            return null;
        }

        var normalizedIndex = index % _playlistQueue.Count;
        if (normalizedIndex < 0)
        {
            normalizedIndex += _playlistQueue.Count;
        }

        _playlistQueue.CurrentIndex = normalizedIndex;
        var track = _playlistQueue[_playlistQueue.CurrentIndex];

        if (RemoveIfInvalid(track))
        {
            return null;
        }
        
        return track;
    }

    public AudioModel? Shuffle(AudioModel? currentTrack)
    {
        if (!_playlistQueue.Any())
        {
            return null;
        }

        _playlistQueue.Shuffle();

        var currentIndex = _playlistQueue.IndexOf(currentTrack);
        if (currentIndex > 0)
        {
            _playlistQueue.Move(currentIndex, 0);
        }

        _playlistQueue.CurrentIndex = 0;
        return _playlistQueue[0];
    }

    public bool RemoveTrack(AudioModel track)
    {
        var removeIndex = _playlistQueue.IndexOf(track);
        if (removeIndex < 0)
        {
            return false;
        }

        var removed = _playlistQueue.Remove(track);
        if (!removed)
        {
            return false;
        }

        if (_playlistQueue.Count == 0)
        {
            _playlistQueue.CurrentIndex = 0;
            return true;
        }

        if (removeIndex < _playlistQueue.CurrentIndex)
        {
            _playlistQueue.CurrentIndex--;
        }
        else if (_playlistQueue.CurrentIndex >= _playlistQueue.Count)
        {
            _playlistQueue.CurrentIndex = 0;
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
        if (!_playlistQueue.Any())
        {
            _playlistQueue.CurrentIndex = 0;
            return false;
        }

        if (File.Exists(audio.Path))
        {
            return false;
        }

        var index = _playlistQueue.IndexOf(audio);
        _playlistQueue.Remove(audio);
        if (index < _playlistQueue.CurrentIndex)
        {
            _playlistQueue.CurrentIndex--;
        }

        if (!_playlistQueue.Any())
        {
            _playlistQueue.CurrentIndex = 0;
            return false;
        }

        NormalizeCurrentIndex();
        return true;
    }

    private void NormalizeCurrentIndex()
    {
        if (_playlistQueue.Count == 0 || _playlistQueue.CurrentIndex < 0)
        {
            _playlistQueue.CurrentIndex = 0;
            return;
        }

        if (_playlistQueue.CurrentIndex >= _playlistQueue.Count)
        {
            _playlistQueue.CurrentIndex = _playlistQueue.Count - 1;
        }
    }
}