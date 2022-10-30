using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class NewSongWindowViewModel
    : INotificationHandler<CurrentSongNotification>
{
    [ObservableProperty] private AudioModel _song = new();

    #region Implementation of INotificationHandler<in CurrentSongNotification>
    /// <inheritdoc />
    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        Song = notification.Audio;
        return Task.CompletedTask;
    }
    #endregion
}