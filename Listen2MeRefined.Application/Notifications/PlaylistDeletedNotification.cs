using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class PlaylistDeletedNotification(int playlistId) : INotification
{
    public int PlaylistId { get; } = playlistId;
}
