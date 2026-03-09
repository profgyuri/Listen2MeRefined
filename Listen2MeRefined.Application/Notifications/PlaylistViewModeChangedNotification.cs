using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class PlaylistViewModeChangedNotification : INotification
{
    public PlaylistViewModeChangedNotification(bool useCompactPlaylistView)
    {
        UseCompactPlaylistView = useCompactPlaylistView;
    }

    public bool UseCompactPlaylistView { get; }
}
