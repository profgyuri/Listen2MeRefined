namespace Listen2MeRefined.Infrastructure.Mvvm;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Diagnostics;

public sealed partial class MainWindowViewModel : 
    ObservableObject,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<CurrentSongNotification>
{
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
        IVersionChecker versionChecker,
        SearchbarViewModel searchbarViewModel,
        PlayerControlsViewModel playerControlsViewModel,
        ListsViewModel listsViewModel,
        StartupManager startupManager)
    {
        _versionChecker = versionChecker ?? throw new ArgumentNullException(nameof(versionChecker));
        _startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));

        _searchbarViewModel = searchbarViewModel;
        _playerControlsViewModel = playerControlsViewModel;
        _listsViewModel = listsViewModel;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var isLatest = await _versionChecker.IsLatestAsync().ConfigureAwait(true);
            IsUpdateExclamationMarkVisible = !isLatest;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Version check failed: {ex}");
        }

        try
        {
            await _startupManager.StartAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"StartupManager.StartAsync failed: {ex}");
        }
    }

    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        Song = notification.Audio;
        return Task.CompletedTask;
    }

    #region Implementation of INotificationHandler<in FontFamilyChangedNotification>
    /// <inheritdoc />
    Task INotificationHandler<FontFamilyChangedNotification>.Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        return Task.CompletedTask;
    }
    #endregion
}