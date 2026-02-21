using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services.Contracts;
using Listen2MeRefined.Infrastructure.Startup;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

public sealed partial class MainWindowViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<CurrentSongNotification>
{
    private readonly ILogger _logger;
    private readonly IUiDispatcher _ui;
    private readonly IAppUpdateCheckService _appUpdateCheckService;
    private readonly IAppSettingsReadService _settingsReadService;
    private readonly StartupManager _startupManager;
    private readonly IMainWindowNavigationService _navigationService;

    public SearchbarViewModel SearchbarViewModel { get; }
    public PlayerControlsViewModel PlayerControlsViewModel { get; }
    public ListsViewModel ListsViewModel { get; }
    public PlaylistPaneViewModel PlaylistPaneViewModel { get; }
    public SearchResultsPaneViewModel SearchResultsPaneViewModel { get; }

    [ObservableProperty] private AudioModel _song = new()
    {
        Artist = "Artist",
        Title = "Title",
        Genre = "Genre",
        Path = ""
    };

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private bool _canNavigateToAuxiliaryWindows = true;

    public MainWindowViewModel(
        ILogger logger,
        IUiDispatcher ui,
        IAppUpdateCheckService appUpdateCheckService,
        IAppSettingsReadService settingsReadService,
        SearchbarViewModel searchbarViewModel,
        PlayerControlsViewModel playerControlsViewModel,
        ListsViewModel listsViewModel,
        PlaylistPaneViewModel playlistPaneViewModel,
        SearchResultsPaneViewModel searchResultsPaneViewModel,
        StartupManager startupManager,
        IMainWindowNavigationService navigationService)
    {
        _logger = logger;
        _ui = ui;
        _appUpdateCheckService = appUpdateCheckService;
        _settingsReadService = settingsReadService;
        _startupManager = startupManager;
        _navigationService = navigationService;

        SearchbarViewModel = searchbarViewModel;
        PlayerControlsViewModel = playerControlsViewModel;
        ListsViewModel = listsViewModel;
        PlaylistPaneViewModel = playlistPaneViewModel;
        SearchResultsPaneViewModel = searchResultsPaneViewModel;

        _logger.Debug("[MainWindowViewModel] Class initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        _logger.Debug("[MainWindowViewModel] Starting InitializeCoreAsync...");
        if (_settingsReadService.GetAutoCheckUpdatesOnStartup())
        {
            _logger.Information("[MainWindowViewModel] Checking for latest version...");
            var status = await _appUpdateCheckService.CheckForUpdatesAsync();
            await _ui.InvokeAsync<bool>(() => IsUpdateAvailable = status.IsUpdateAvailable, ct);

            _logger.Information<bool>("[MainWindowViewModel] Version check completed. Update available: {IsUpdateAvailable}", IsUpdateAvailable);
        }
        else
        {
            await _ui.InvokeAsync<bool>(() => IsUpdateAvailable = false, ct);
            _logger.Information("[MainWindowViewModel] Automatic update checks are disabled.");
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

        _logger.Debug("[MainWindowViewModel] Finished InitializeCoreAsync");
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToAuxiliaryWindows))]
    private async Task OpenSettingsWindow()
    {
        await NavigateAuxiliaryAsync(async () =>
        {
            IsUpdateAvailable = false;
            await _navigationService.OpenSettingsAsync();
        });
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToAuxiliaryWindows))]
    private async Task OpenAdvancedSearchWindow()
    {
        await NavigateAuxiliaryAsync(async () =>
        {
            await _navigationService.OpenAdvancedSearchAsync();
            ListsViewModel.SwitchToSearchResultsTab();
        });
    }

    partial void OnCanNavigateToAuxiliaryWindowsChanged(bool value)
    {
        OpenSettingsWindowCommand.NotifyCanExecuteChanged();
        OpenAdvancedSearchWindowCommand.NotifyCanExecuteChanged();
    }

    private async Task NavigateAuxiliaryAsync(Func<Task> action)
    {
        if (!CanNavigateToAuxiliaryWindows)
        {
            return;
        }

        CanNavigateToAuxiliaryWindows = false;
        try
        {
            await action();
        }
        finally
        {
            CanNavigateToAuxiliaryWindows = true;
        }
    }

    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[MainWindowViewModel] Received CurrentSongNotification: {Audio}", notification.Audio);
        return _ui.InvokeAsync<AudioModel>(() => Song = notification.Audio, cancellationToken);
    }

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[MainWindowViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        return _ui.InvokeAsync<string>(() => FontFamily = notification.FontFamily, cancellationToken);
    }
}
