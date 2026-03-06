namespace Listen2MeRefined.Infrastructure.Playlist;

public sealed class PlaylistRenamedNotification(int playlistId, string name) : INotification
{
    public int PlaylistId { get; } = playlistId;
    public string Name { get; } = name;
}
