using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Order;
using Listen2MeRefined.Application.Playlist.Queuing;
using Listen2MeRefined.Application.Playlist.Store;

namespace Listen2MeRefined.Infrastructure.Playlist;

public class PlaylistService : IPlaylistService
{
    public PlaylistService(IPlaylistOrder playlistOrder, IPlaylistManager playlistManager, IPlaylistQueuing playlistQueuing)
    {
        Order = playlistOrder;
        Manager = playlistManager;
        Queuing = playlistQueuing;
    }

    public IPlaylistOrder Order { get; }
    public IPlaylistManager Manager { get; }
    public IPlaylistQueuing Queuing { get; }
}