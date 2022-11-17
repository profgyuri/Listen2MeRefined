using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class NewSongWindowViewModel
    : INotificationHandler<CurrentSongNotification>,
        INotificationHandler<FontFamilyChangedNotification>
{
    [ObservableProperty] private AudioModel _song = new();
    [ObservableProperty] private string _fontFamily = default!;

    #region Implementation of INotificationHandler<in CurrentSongNotification>
    /// <inheritdoc />
    public Task Handle(
        CurrentSongNotification notification,
        CancellationToken cancellationToken)
    {
        Song = notification.Audio;
        return Task.CompletedTask;
    }
    #endregion

    #region Implementation of INotificationHandler<in FontFamilyChangedNotification>
    /// <inheritdoc />
    public Task Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        return Task.CompletedTask;
    }
    #endregion
}