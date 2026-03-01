namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class PlaylistMembershipChangedNotification(int playlistId) : INotification
{
    public int PlaylistId { get; } = playlistId;
}
