using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist.Order;

/// <summary>
///     Manipulates the ordering of the items in a playlist.
/// </summary>
public interface IPlaylistOrder
{
    /// <summary>
    ///     Sorts a playlist by a given property.
    /// </summary>
    /// <param name="songs">The collection to sort</param>
    /// <param name="property">The property to sort by</param>
    /// <param name="direction">Either descending or ascending</param>
    void Sort(ObservableCollection<AudioModel> songs, PlaylistSortProperty property, SortDirection direction);
    
    /// <summary>
    ///     Randomizes the order of the items in the active playlist.
    /// </summary>
    void Shuffle();
}