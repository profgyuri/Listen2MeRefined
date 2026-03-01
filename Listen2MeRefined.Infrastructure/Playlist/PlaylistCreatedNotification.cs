namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class PlaylistCreatedNotification(int playlistId, string name) : INotification
{
    public int PlaylistId { get; } = playlistId;
    public string Name { get; } = name;
}
