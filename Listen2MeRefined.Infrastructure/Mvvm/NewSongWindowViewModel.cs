using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services.Contracts;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

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

    private readonly IAppSettingsReadService _settingsReadService;
    private readonly IWindowPositionPolicyService _windowPositionPolicyService;
    private readonly ILogger _logger;

    public bool IsTopmost { get; set; }

    public NewSongWindowViewModel(
        IAppSettingsReadService settingsReadService,
        IWindowPositionPolicyService windowPositionPolicyService,
        ILogger logger)
    {
        _settingsReadService = settingsReadService;
        _windowPositionPolicyService = windowPositionPolicyService;
        _logger = logger;

        _logger.Debug("[NewSongWindowViewModel] initialized");
    }

    protected override Task InitializeCoreAsync(CancellationToken ct)
    {
        IsTopmost = _windowPositionPolicyService.IsTopmost(_settingsReadService.GetNewSongWindowPosition());

        _logger.Debug("[NewSongWindowViewModel] Finished InitializeCoreAsync");

        return base.InitializeCoreAsync(ct);
    }

    public Task Handle(NewSongWindowPositionChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[NewSongWindowViewModel] Received NewSongWindowPositionChangedNotification: {Position}", notification.Position);
        IsTopmost = _windowPositionPolicyService.IsTopmost(notification.Position);
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
