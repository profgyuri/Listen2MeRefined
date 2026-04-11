using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Sorts an <see cref="ObservableCollection{AudioModel}"/> in place
/// by a given property and direction.
/// </summary>
public interface IPlaylistSortService
{
    void Sort(ObservableCollection<AudioModel> songs, PlaylistSortProperty property, SortDirection direction);
}
