namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class PlaylistDeletedNotification(int playlistId) : INotification
{
    public int PlaylistId { get; } = playlistId;
}
