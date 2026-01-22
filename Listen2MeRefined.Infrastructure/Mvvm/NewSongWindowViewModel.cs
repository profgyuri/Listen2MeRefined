namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class NewSongWindowViewModel :
    ViewModelBase,
    INotificationHandler<NewSongWindowPositionChangedNotification>,
    INotificationHandler<CurrentSongNotification>
{
    [ObservableProperty] private AudioModel _song = new()
    {
        Artist = "Artist",
        Title = "Title",
        Genre = "Genre",
        Path = ""
    };

    private readonly ISettingsManager<AppSettings> _settingsManager;

    public bool IsTopmost { get; set; }

    public NewSongWindowViewModel(ISettingsManager<AppSettings> settingsManager)
    {
        _settingsManager = settingsManager;
    }

    protected override Task InitializeCoreAsync(CancellationToken ct)
    {
        IsTopmost = _settingsManager.Settings.NewSongWindowPosition == "Always on top";

        return base.InitializeCoreAsync(ct);
    }

    public Task Handle(NewSongWindowPositionChangedNotification notification, CancellationToken cancellationToken)
    {
        IsTopmost = notification.Position == "Always on top";
        OnPropertyChanged(nameof(IsTopmost));
        return Task.CompletedTask;
    }

    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        Song = notification.Audio;
        return Task.CompletedTask;
    }
}