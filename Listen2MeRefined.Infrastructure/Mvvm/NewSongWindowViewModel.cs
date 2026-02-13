namespace Listen2MeRefined.Infrastructure.Mvvm;

using System.Threading;
using System.Threading.Tasks;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;

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
    private readonly ILogger _logger;

    public bool IsTopmost { get; set; }

    public NewSongWindowViewModel(ISettingsManager<AppSettings> settingsManager, ILogger logger)
    {
        _settingsManager = settingsManager;
        _logger = logger;

        _logger.Debug("[NewSongWindowViewModel] initialized");
    }

    protected override Task InitializeCoreAsync(CancellationToken ct)
    {
        IsTopmost = _settingsManager.Settings.NewSongWindowPosition == "Always on top";

        _logger.Debug("[NewSongWindowViewModel] Finished InitializeCoreAsync");

        return base.InitializeCoreAsync(ct);
    }

    public Task Handle(NewSongWindowPositionChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[NewSongWindowViewModel] Received NewSongWindowPositionChangedNotification: {Position}", notification.Position);
        IsTopmost = notification.Position == "Always on top";
        OnPropertyChanged(nameof(IsTopmost));
        return Task.CompletedTask;
    }

    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[NewSongWindowViewModel] Received CurrentSongNotification: {@Audio}", notification.Audio);
        Song = notification.Audio;
        return Task.CompletedTask;
    }
}