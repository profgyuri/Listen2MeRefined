namespace Listen2MeRefined.Infrastructure.Mvvm;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Diagnostics;

public sealed partial class MainWindowViewModel : 
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<CurrentSongNotification>
{
    private readonly IUiDispatcher _ui;
    private readonly IVersionChecker _versionChecker;
    private readonly StartupManager _startupManager;
    
    [ObservableProperty] private SearchbarViewModel _searchbarViewModel;
    [ObservableProperty] private PlayerControlsViewModel _playerControlsViewModel;
    [ObservableProperty] private ListsViewModel _listsViewModel;
    [ObservableProperty] private AudioModel _song = new()
    {
        Artist = "Artist",
        Title = "Title",
        Genre = "Genre",
        Path = ""
    };

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private bool _isUpdateExclamationMarkVisible;

    public MainWindowViewModel(
        IUiDispatcher ui,
        IVersionChecker versionChecker,
        SearchbarViewModel searchbarViewModel,
        PlayerControlsViewModel playerControlsViewModel,
        ListsViewModel listsViewModel,
        StartupManager startupManager)
    {
        _ui = ui;
        _versionChecker = versionChecker;
        _startupManager = startupManager;
        _searchbarViewModel = searchbarViewModel;
        _playerControlsViewModel = playerControlsViewModel;
        _listsViewModel = listsViewModel;
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        try
        {
            var isLatest = await _versionChecker.IsLatestAsync();
            await _ui.InvokeAsync(() => IsUpdateExclamationMarkVisible = !isLatest, ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Version check failed: {ex}");
        }

        try
        {
            await _startupManager.StartAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"StartupManager.StartAsync failed: {ex}");
        }
    }

    /// <inheritdoc />
    Task INotificationHandler<CurrentSongNotification>.Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        return _ui.InvokeAsync(() => Song = notification.Audio, cancellationToken);
    }

    /// <inheritdoc />
    Task INotificationHandler<FontFamilyChangedNotification>.Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        return _ui.InvokeAsync(() => FontFamily = notification.FontFamily, cancellationToken);
    }
}