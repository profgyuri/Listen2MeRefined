namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Mvvm.MainWindow;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

public sealed partial class MainWindowViewModel : 
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<CurrentSongNotification>
{
    private readonly ILogger _logger;
    private readonly IUiDispatcher _ui;
    private readonly IVersionChecker _versionChecker;
    private readonly StartupManager _startupManager;
    
    public SearchbarViewModel SearchbarViewModel { get; }
    public PlayerControlsViewModel PlayerControlsViewModel { get; }
    public ListsViewModel ListsViewModel { get; }

    [ObservableProperty] private AudioModel _song = new()
    {
        Artist = "Artist",
        Title = "Title",
        Genre = "Genre",
        Path = ""
    };
    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private bool _isUpdateAvailable;

    public MainWindowViewModel(
        ILogger logger,
        IUiDispatcher ui,
        IVersionChecker versionChecker,
        SearchbarViewModel searchbarViewModel,
        PlayerControlsViewModel playerControlsViewModel,
        ListsViewModel listsViewModel,
        StartupManager startupManager)
    {
        _logger = logger;
        _ui = ui;
        _versionChecker = versionChecker;
        _startupManager = startupManager;

        SearchbarViewModel = searchbarViewModel;
        PlayerControlsViewModel = playerControlsViewModel;
        ListsViewModel = listsViewModel;

        _logger.Information("[MainWindowViewModel] Class initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        _logger.Information("[MainWindowViewModel] Starting InitializeCoreAsync...");
        try
        {
            _logger.Information("[MainWindowViewModel] Checking for latest version...");

            var isLatest = await _versionChecker.IsLatestAsync();
            await _ui.InvokeAsync(() => IsUpdateAvailable = !isLatest, ct);

            _logger.Information("[MainWindowViewModel] Version check completed. Update available: {IsUpdateAvailable}", IsUpdateAvailable);
        }
        catch (Exception ex)
        {
            _logger.Warning($"[MainWindowViewModel] Version check failed: {ex}");
            await _ui.InvokeAsync(() => IsUpdateAvailable = false, ct);
        }

        try
        {
            await _startupManager.StartAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.Information("[MainWindowViewModel] StartupManager.StartAsync was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "[MainWindowViewModel] StartupManager.StartAsync failed");
            throw;
        }
    }

    /// <inheritdoc />
    Task INotificationHandler<CurrentSongNotification>.Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[MainWindowViewModel] Received CurrentSongNotification: {Audio}", notification.Audio);
        return _ui.InvokeAsync(() => Song = notification.Audio, cancellationToken);
    }

    /// <inheritdoc />
    Task INotificationHandler<FontFamilyChangedNotification>.Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.Information("[MainWindowViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        return _ui.InvokeAsync(() => FontFamily = notification.FontFamily, cancellationToken);
    }
}