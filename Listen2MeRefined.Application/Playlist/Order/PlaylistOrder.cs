using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Listen2MeRefined.Application.Playlist.Queuing;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist.Order;

public class PlaylistOrder : IPlaylistOrder
{
    private readonly IPlaylistQueuing _playlistQueuing;

    public PlaylistOrder(IPlaylistQueuing playlistQueuing)
    {
        _playlistQueuing = playlistQueuing;
    }

    public void Sort(ObservableCollection<AudioModel> songs, PlaylistSortProperty property, SortDirection direction)
    {
        if (songs.Count <= 1)
        {
            return;
        }

        var keySelector = GetSortKeySelector(property);
        var sorted = direction == SortDirection.Ascending
            ? songs.OrderBy(keySelector).ToList()
            : songs.OrderByDescending(keySelector).ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            var currentIndex = songs.IndexOf(sorted[i]);
            if (currentIndex != i)
            {
                songs.Move(currentIndex, i);
            }
        }
    }

    public void Shuffle()
    {
        var queue = _playlistQueuing.ActiveQueue;
        
        if (!ShuffleOrder(queue.Items)) return;
        
        var selected = _playlistQueuing.SelectedSong;
        var currentIndex = queue.IndexOf(selected);

        if (currentIndex >= 0)
        {
            queue.Move(currentIndex, 0);
        }
        
        queue.CurrentIndex = 0;
        _playlistQueuing.SelectedSong = queue[0];
    }

    private bool ShuffleOrder(ObservableCollection<AudioModel> songs)
    {
        if (songs.Count <= 1)
        {
            return false;
        }
        
        var n = songs.Count;

        while (n > 1)
        {
            var k = RandomNumberGenerator.GetInt32(n);
            n--;

            (songs[k], songs[n]) = (songs[n], songs[k]);
        }

        return true;
    }

    private Func<AudioModel, IComparable> GetSortKeySelector(PlaylistSortProperty property) =>
        property switch
        {
            PlaylistSortProperty.Artist => a => a.Artist ?? string.Empty,
            PlaylistSortProperty.Title => a => a.Title ?? string.Empty,
            PlaylistSortProperty.Duration => a => a.Length,
            PlaylistSortProperty.Genre => a => a.Genre ?? string.Empty,
            PlaylistSortProperty.BPM => a => a.BPM,
            PlaylistSortProperty.Bitrate => a => a.Bitrate,
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
}