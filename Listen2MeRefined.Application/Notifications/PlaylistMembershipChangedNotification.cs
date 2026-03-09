using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class PlaylistMembershipChangedNotification(int playlistId) : INotification
{
    public int PlaylistId { get; } = playlistId;
}
