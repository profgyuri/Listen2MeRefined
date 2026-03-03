namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class PlaylistViewModeChangedNotification : INotification
{
    public PlaylistViewModeChangedNotification(bool useCompactPlaylistView)
    {
        UseCompactPlaylistView = useCompactPlaylistView;
    }

    public bool UseCompactPlaylistView { get; }
}
