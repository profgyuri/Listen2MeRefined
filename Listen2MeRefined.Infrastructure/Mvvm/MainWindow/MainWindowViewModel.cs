using Listen2MeRefined.Core.Interfaces.System;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class MainWindowViewModel : 
    ObservableObject,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IAdvancedDataReader<ParameterizedQuery, AudioModel> _advancedAudioReader;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IGlobalHook _globalHook;
    private readonly IFolderScanner _folderScanner;
    private readonly DataContext _dataContext;
    private readonly IVersionChecker _versionChecker;

    [ObservableProperty] private SearchbarViewModel _searchbarViewModel;
    [ObservableProperty] private PlayerControlsViewModel _playerControlsViewModel;
    [ObservableProperty] private ListsViewModel _listsViewModel;

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private bool _isUpdateExclamationMarkVisible;

    public MainWindowViewModel(
        ILogger logger,
        IAdvancedDataReader<ParameterizedQuery, AudioModel> advancedAudioReader,
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook,
        IFolderScanner folderScanner,
        DataContext dataContext,
        IVersionChecker versionChecker,
        SearchbarViewModel searchbarViewModel,
        PlayerControlsViewModel playerControlsViewModel,
        ListsViewModel listsViewModel)
    {
        _logger = logger;
        _advancedAudioReader = advancedAudioReader;
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _globalHook.Register();
        _folderScanner = folderScanner;
        _dataContext = dataContext;
        _versionChecker = versionChecker;

        _searchbarViewModel = searchbarViewModel;
        _playerControlsViewModel = playerControlsViewModel;
        _listsViewModel = listsViewModel;

        AsyncInit().ConfigureAwait(false);
    }

    private async Task AsyncInit()
    {
        await Task.Run(async () => await _dataContext.Database.MigrateAsync());
        await Task.Run(async () =>
        {
            FontFamily = _settingsManager.Settings.FontFamily;
            IsUpdateExclamationMarkVisible = !await _versionChecker.IsLatestAsync();
        });
        
        if (_settingsManager.Settings.ScanOnStartup)
        {
            await Task.Run(async () => await _folderScanner.ScanAllAsync());
        }
    }
    
    ~MainWindowViewModel()
    {
        _globalHook.Unregister();
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