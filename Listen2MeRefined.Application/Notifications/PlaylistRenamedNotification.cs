using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class PlaylistRenamedNotification(int playlistId, string name) : INotification
{
    public int PlaylistId { get; } = playlistId;
    public string Name { get; } = name;
}
