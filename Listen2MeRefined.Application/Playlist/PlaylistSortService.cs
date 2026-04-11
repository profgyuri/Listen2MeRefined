using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <inheritdoc />
public sealed class PlaylistSortService : IPlaylistSortService
{
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

    internal static Func<AudioModel, IComparable> GetSortKeySelector(PlaylistSortProperty property) =>
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
