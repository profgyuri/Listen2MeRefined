using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class MainWindowViewModel : 
    ObservableObject,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly IVersionChecker _versionChecker;

    [ObservableProperty] private SearchbarViewModel _searchbarViewModel;
    [ObservableProperty] private PlayerControlsViewModel _playerControlsViewModel;
    [ObservableProperty] private ListsViewModel _listsViewModel;

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private bool _isUpdateExclamationMarkVisible;

    public MainWindowViewModel(
        IVersionChecker versionChecker,
        SearchbarViewModel searchbarViewModel,
        PlayerControlsViewModel playerControlsViewModel,
        ListsViewModel listsViewModel,
        StartupManager startupManager)
    {
        _versionChecker = versionChecker;

        _searchbarViewModel = searchbarViewModel;
        _playerControlsViewModel = playerControlsViewModel;
        _listsViewModel = listsViewModel;

        Task.Run(async () => IsUpdateExclamationMarkVisible = !await _versionChecker.IsLatestAsync());
        Task.Run(startupManager.StartAsync);
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