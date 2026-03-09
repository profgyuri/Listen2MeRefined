using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class NewSongWindowPositionChangedNotification : INotification
{
    public string Position { get; set; }

    public NewSongWindowPositionChangedNotification(string position)
    {
        Position = position;
    }
}
