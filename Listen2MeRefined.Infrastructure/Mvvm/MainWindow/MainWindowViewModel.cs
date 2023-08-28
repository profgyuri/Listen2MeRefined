using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class MainWindowViewModel : 
    ObservableObject,
    INotificationHandler<FontFamilyChangedNotification>
{
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
        StartupManager startupManager,
        IGlobalHook globalHook)
    {
        _searchbarViewModel = searchbarViewModel;
        _playerControlsViewModel = playerControlsViewModel;
        _listsViewModel = listsViewModel;

        Task.Run(async () => IsUpdateExclamationMarkVisible = !await versionChecker.IsLatestAsync());
        Task.Run(startupManager.StartAsync);

        globalHook.Register();
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