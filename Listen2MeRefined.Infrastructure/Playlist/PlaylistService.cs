using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Order;
using Listen2MeRefined.Application.Playlist.Queuing;
using Listen2MeRefined.Application.Playlist.Store;

namespace Listen2MeRefined.Infrastructure.Playlist;

public class PlaylistService : IPlaylistService
{
    public PlaylistService(IOrder order, IStore store, IQueuing queuing)
    {
        Order = order;
        Store = store;
        Queuing = queuing;
    }

    public IOrder Order { get; }
    public IStore Store { get; }
    public IQueuing Queuing { get; }
}