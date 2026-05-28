using Listen2MeRefined.Application.Playlist.Order;
using Listen2MeRefined.Application.Playlist.Queuing;
using Listen2MeRefined.Application.Playlist.Store;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
///     Master playlist interface used to call all playlist-related services.
/// </summary>
public interface IPlaylistService
{
    /// <summary>
    /// Gets the methods for ordering the playlist.
    /// </summary>
    IPlaylistOrder Order { get; }
    
    /// <summary>
    /// Gets the methods for handling the existence of playlists.
    /// </summary>
    IPlaylistManager Manager { get; }
    
    /// <summary>
    /// Gets the services used for handling individual playlists.
    /// </summary>
    IPlaylistQueuing Queuing { get; }
}