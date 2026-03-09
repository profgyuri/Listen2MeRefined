using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Controls;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using MediatR;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Windows;

public sealed partial class MainWindowViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<PlayerStateChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IUiDispatcher _ui;
    private readonly IAppUpdateChecker _appUpdateChecker;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;
    private readonly IStartupManager _startupManager;
    private readonly IMainWindowNavigationService _navigationService;

    public SearchbarViewModel SearchbarViewModel { get; }
    public PlaybackControlsViewModel PlaybackControlsViewModel { get; }
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
    
    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private bool _canNavigateToAuxiliaryWindows = true;
    [ObservableProperty] private bool _isTaskStatusVisible;
    [ObservableProperty] private string _taskStatusText = "";
    [ObservableProperty] private string _taskStatusTooltip = "";
    [ObservableProperty] private PlayerState _playerState = PlayerState.Stopped;

    public MainWindowViewModel(
        ILogger logger,
        IUiDispatcher ui,
        IAppUpdateChecker appUpdateChecker,
        IAppSettingsReader settingsReader,
        IBackgroundTaskStatusService backgroundTaskStatusService,
        SearchbarViewModel searchbarViewModel,
        PlaybackControlsViewModel playbackControlsViewModel,
        ListsViewModel listsViewModel,
        PlaylistPaneViewModel playlistPaneViewModel,
        SearchResultsPaneViewModel searchResultsPaneViewModel,
        IStartupManager startupManager,
        IMainWindowNavigationService navigationService)
    {
        _logger = logger;
        _ui = ui;
        _appUpdateChecker = appUpdateChecker;
        _settingsReader = settingsReader;
        _backgroundTaskStatusService = backgroundTaskStatusService;
        _startupManager = startupManager;
        _navigationService = navigationService;

        SearchbarViewModel = searchbarViewModel;
        PlaybackControlsViewModel = playbackControlsViewModel;
        ListsViewModel = listsViewModel;
        PlaylistPaneViewModel = playlistPaneViewModel;
        SearchResultsPaneViewModel = searchResultsPaneViewModel;
        _backgroundTaskStatusService.SnapshotChanged += BackgroundTaskStatusServiceOnSnapshotChanged;
        ApplyTaskSnapshot(_backgroundTaskStatusService.GetSnapshot());

        _logger.Debug("[MainWindowViewModel] Class initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        _logger.Debug("[MainWindowViewModel] Starting InitializeCoreAsync...");
        
        try
        {
            await _startupManager.StartAsync(ct);
            await PlaylistPaneViewModel.InitializeAsync(ct);
            await PlaybackControlsViewModel.InitializeAsync(ct);
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
        
        if (_settingsReader.GetAutoCheckUpdatesOnStartup())
        {
            _logger.Information("[MainWindowViewModel] Checking for latest version...");
            var status = await _appUpdateChecker.CheckForUpdatesAsync();
            await _ui.InvokeAsync(() => IsUpdateAvailable = status.IsUpdateAvailable, ct);

            _logger.Information("[MainWindowViewModel] Version check completed. Update available: {IsUpdateAvailable}", IsUpdateAvailable);
        }
        else
        {
            await _ui.InvokeAsync(() => IsUpdateAvailable = false, ct);
            _logger.Information("[MainWindowViewModel] Automatic update checks are disabled.");
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

    private void BackgroundTaskStatusServiceOnSnapshotChanged(object? sender, BackgroundTaskSnapshot snapshot)
    {
        _ = _ui.InvokeAsync(() => ApplyTaskSnapshot(snapshot));
    }

    private void ApplyTaskSnapshot(BackgroundTaskSnapshot snapshot)
    {
        IsTaskStatusVisible = snapshot.IsVisible && snapshot.PrimaryTask is not null;
        if (!IsTaskStatusVisible)
        {
            TaskStatusText = string.Empty;
            TaskStatusTooltip = string.Empty;
            return;
        }

        TaskStatusText = FormatTaskStatusText(snapshot.PrimaryTask!, snapshot.QueuedCount);
        TaskStatusTooltip = FormatTaskStatusTooltip(snapshot.PrimaryTask!, TaskStatusText);
    }

    private static string FormatTaskStatusText(BackgroundTaskItem task, int queuedCount)
    {
        var parts = new List<string>();

        if (task.State == BackgroundTaskState.Completed)
        {
            parts.Add("Done");
        }
        else if (task.State == BackgroundTaskState.Failed)
        {
            parts.Add("Error");
        }

        parts.Add(task.DisplayName);

        if (task.Percent is not null)
        {
            parts.Add($"{task.Percent}%");
        }

        if (queuedCount > 0)
        {
            parts.Add($"+{queuedCount}");
        }

        return string.Join(" | ", parts);
    }

    private static string FormatTaskStatusTooltip(BackgroundTaskItem task, string fallbackText)
    {
        return string.IsNullOrWhiteSpace(task.Message)
            ? fallbackText
            : task.Message!;
    }
    
    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[MainWindowViewModel] Received CurrentSongNotification: {Audio}", notification.Audio);
        return _ui.InvokeAsync<AudioModel>(() => Song = notification.Audio, cancellationToken);
    }

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[MainWindowViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        return _ui.InvokeAsync(() => FontFamilyName = notification.FontFamily, cancellationToken);
    }

    public Task Handle(PlayerStateChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[MainWindowViewModel] Received PlayerStateChangedNotification: {PlayerState}", notification.PlayerState);
        PlayerState = notification.PlayerState;
        return Task.CompletedTask;
    }
}
