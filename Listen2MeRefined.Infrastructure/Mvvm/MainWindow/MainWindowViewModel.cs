using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Interfaces;
using Listen2MeRefined.Core.Interfaces.System;
using Listen2MeRefined.Core.Source;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NAudio.Utils;
using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class MainWindowViewModel : 
    ObservableObject,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>,
    INotificationHandler<QuickSearchResultsNotification>
{
    private readonly ILogger _logger;
    private readonly IPlaylistReference _playlistReference;
    private readonly IAdvancedDataReader<ParameterizedQuery, AudioModel> _advancedAudioReader;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IGlobalHook _globalHook;
    private readonly IFolderScanner _folderScanner;
    private readonly DataContext _dataContext;
    private readonly IVersionChecker _versionChecker;
    private readonly IFileScanner _fileScanner;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    private readonly HashSet<AudioModel> _selectedPlaylistItems = new();
    private readonly IMediaController<SKBitmap> _mediaController;

    [ObservableProperty] private SearchbarViewModel _searchbarViewModel;
    [ObservableProperty] private PlayerControlsViewModel _playerControlsViewModel;

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private string _searchTerm = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private ObservableCollection<AudioModel> _playList = new();
    [ObservableProperty] private bool _isUpdateExclamationMarkVisible;
    [ObservableProperty] private bool _isSearchResultsTabVisible = true;
    [ObservableProperty] private bool _isSongMenuTabVisible;

    private int _currentSongIndex = -1;

    public MainWindowViewModel(
        ILogger logger,
        IPlaylistReference playlistReference,
        IAdvancedDataReader<ParameterizedQuery, AudioModel> advancedAudioReader,
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook,
        IFolderScanner folderScanner,
        DataContext dataContext,
        IVersionChecker versionChecker,
        IFileScanner fileScanner,
        SearchbarViewModel searchbarViewModel,
        PlayerControlsViewModel playerControlsViewModel,
        IMediaController<SKBitmap> mediaController)
    {
        _logger = logger;
        _playlistReference = playlistReference;
        _advancedAudioReader = advancedAudioReader;
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _folderScanner = folderScanner;
        _dataContext = dataContext;
        _versionChecker = versionChecker;
        _fileScanner = fileScanner;
        _mediaController = mediaController;

        _searchbarViewModel = searchbarViewModel;
        _playerControlsViewModel = playerControlsViewModel;

        AsyncInit().ConfigureAwait(false);
        Init();
    }

    private void Init()
    {
        _playlistReference.PassPlaylist(ref _playList);
        _globalHook.Register();
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

    #region Implementation of INotificationHandler<in CurrentSongNotification>
    /// <inheritdoc />
    async Task INotificationHandler<CurrentSongNotification>.Handle(
        CurrentSongNotification notification,
        CancellationToken cancellationToken)
    {
        SelectedSong = notification.Audio;
        _currentSongIndex = PlayList.IndexOf(SelectedSong);
    }
    #endregion
    
    #region Implementation of INotificationHandler<in AdvancedSearchNotification>
    /// <inheritdoc />
    public async Task Handle(
        AdvancedSearchNotification notification,
        CancellationToken cancellationToken)
    {
        var result = 
            await _advancedAudioReader.ReadAsync(notification.Filters, notification.MatchAll);
        SearchResults.Clear();
        SearchResults.AddRange(result);
    }
    #endregion

    public async Task Handle(QuickSearchResultsNotification notification, CancellationToken cancellationToken)
    {
        SwitchToSearchResultsTab();
        SearchResults.Clear();
        SearchResults.AddRange(notification.Results);
        await Task.CompletedTask;
    }

    #region Commands
    [RelayCommand]
    public async Task JumpToSelecteSong()
    {
        if (SelectedIndex > -1)
        {
            await _mediaController.JumpToIndexAsync(SelectedIndex);
        }
    }

    [RelayCommand]
    private void SendSelectedToPlaylist()
    {
        if (!_selectedSearchResults.Any())
        {
            _logger.Verbose("Sending all {Count} search results to the playlist", _selectedSearchResults.Count);
            SendAllToPlaylist();
            return;
        }

        _logger.Verbose("Sending {Count} selected search results to the playlist", _selectedSearchResults.Count);
        PlayList.AddRange(_selectedSearchResults);

        while (_selectedSearchResults.Count > 0)
        {
            var toRemove = _selectedSearchResults.First();
            SearchResults.Remove(toRemove);
            _selectedSearchResults.Remove(toRemove);
        }
    }

    private void SendAllToPlaylist()
    {
        PlayList.AddRange(SearchResults);
        SearchResults.Clear();
        _selectedSearchResults.Clear();
    }

    [RelayCommand]
    private void RemoveSelectedFromPlaylist()
    {
        if (_selectedSearchResults.Count == 0)
        {
            _logger.Verbose($"Removing all item from playlist");
            ClearPlaylist();
            return;
        }

        _logger.Verbose($"Removing selected items from playlist");
        while (_selectedPlaylistItems.Count > 0)
        {
            var toRemove = _selectedPlaylistItems.First();
            PlayList.Remove(toRemove);
            _selectedPlaylistItems.Remove(toRemove);
        }
    }

    private void ClearPlaylist()
    {
        PlayList.Clear();
        _selectedPlaylistItems.Clear();
    }

    [RelayCommand]
    private void SetSelectedSongAsNext()
    {
        if (SelectedSong is null || PlayList.Count <= 1)
        {
            return;
        }

        _logger.Verbose($"Setting {SelectedSong.Title} as next song");
        var selectedSongIndex = PlayList.IndexOf(SelectedSong);
        var newIndex = _currentSongIndex + 1;

        if (newIndex >= PlayList.Count)
        {
            newIndex = 0;
        }

        PlayList.Move(selectedSongIndex, newIndex);
    }

    [RelayCommand]
    private async Task ScanSelectedSong()
    {
        if (SelectedSong is null)
        {
            return;
        }

        _logger.Verbose($"Scanning {SelectedSong.Title}");
        var scanned = await _fileScanner.ScanAsync(SelectedSong.Path!);
        var index = PlayList.IndexOf(SelectedSong);
        PlayList[index] = scanned;
        SelectedSong = scanned;
    }

    [RelayCommand]
    public void SwitchToSearchResultsTab()
    {
        IsSearchResultsTabVisible = true;
        IsSongMenuTabVisible = false;
    }

    [RelayCommand]
    public void SwitchToSongMenuTab()
    {
        IsSearchResultsTabVisible = false;
        IsSongMenuTabVisible = true;
    }
    #endregion

    public void AddSelectedSearchResult(AudioModel song)
    {
        _selectedSearchResults.Add(song);
    }

    public void RemoveSelectedSearchResult(AudioModel song)
    {
        _selectedSearchResults?.Remove(song);
    }

    public void AddSelectedPlaylistItems(AudioModel song)
    {
        _selectedPlaylistItems.Add(song);
    }

    public void RemoveSelectedPlaylistItems(AudioModel song)
    {
        _selectedPlaylistItems.Remove(song);
    }
}