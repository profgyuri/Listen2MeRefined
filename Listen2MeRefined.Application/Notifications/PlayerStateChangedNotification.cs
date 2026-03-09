using Listen2MeRefined.Core.Enums;
using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public class PlayerStateChangedNotification : INotification
{
    public PlayerState PlayerState { get; set; }
    
    public PlayerStateChangedNotification(PlayerState state)
    {
        PlayerState = state;
    }
}