namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class NewSongWindowViewModel :
    ObservableObject,
    INotificationHandler<NewSongWindowPositionChangedNotification>
{
    private readonly ISettingsManager<AppSettings> _settingsManager;

    public bool IsTopmost { get; set; }

    public NewSongWindowViewModel(ISettingsManager<AppSettings> settingsManager)
    {
        _settingsManager = settingsManager;
        IsTopmost = _settingsManager.Settings.NewSongWindowPosition == "Always on top";
    }

    public Task Handle(NewSongWindowPositionChangedNotification notification, CancellationToken cancellationToken)
    {
        IsTopmost = notification.Position == "Always on top";
        OnPropertyChanged(nameof(IsTopmost));
        return Task.CompletedTask;
    }
}