using Listen2MeRefined.Infrastructure.Media.MusicPlayer;

namespace Listen2MeRefined.Infrastructure.Notifications;

public class PlayerStateChangedNotification : INotification
{
    public PlayerState PlayerState { get; set; }
    
    public PlayerStateChangedNotification(PlayerState state)
    {
        PlayerState = state;
    }
}