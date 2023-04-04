using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Source;
using Source.Extensions;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class MainWindowViewModel : 
    ObservableObject,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>
{
    private readonly Guid ID = Guid.NewGuid();
    private readonly ILogger _logger;
    private readonly IPlaylistReference _playlistReference;
    private readonly IMediaController<SKBitmap> _mediaController;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IAdvancedDataReader<ParameterizedQuery, AudioModel> _advancedAudioReader;
    private readonly TimedTask _timedTask;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IGlobalHook _globalHook;
    private readonly IFolderScanner _folderScanner;
    private readonly DataContext _dataContext;
    private readonly IWaveFormDrawer<SKBitmap> _waveFormDrawer;
    private readonly IVersionChecker _versionChecker;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    private readonly HashSet<AudioModel> _selectedPlaylistItems = new();

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private string _searchTerm = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private ObservableCollection<AudioModel> _playList = new();
    [ObservableProperty] private SKBitmap _waveForm = new(1, 1);
    [ObservableProperty] private int _waveFormWidth;
    [ObservableProperty] private int _waveFormHeight;
    [ObservableProperty] private double _totalTime;
    [ObservableProperty] private bool _isUpdateExclamationMarkVisible;

    public double CurrentTime
    {
        get => _mediaController.CurrentTime;
        set
        {
            _mediaController.CurrentTime = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel(
        IMediaController<SKBitmap> mediaController,
        ILogger logger,
        IPlaylistReference playlistReference,
        IRepository<AudioModel> audioRepository,
        IAdvancedDataReader<ParameterizedQuery, AudioModel> advancedAudioReader,
        TimedTask timedTask,
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook,
        IFolderScanner folderScanner,
        DataContext dataContext,
        IWaveFormDrawer<SKBitmap> waveFormDrawer,
        IVersionChecker versionChecker)
    {
        _mediaController = mediaController;
        _logger = logger;
        _playlistReference = playlistReference;
        _audioRepository = audioRepository;
        _advancedAudioReader = advancedAudioReader;
        _timedTask = timedTask;
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _folderScanner = folderScanner;
        _dataContext = dataContext;
        _waveFormDrawer = waveFormDrawer;
        _versionChecker = versionChecker;

        AsyncInit().ConfigureAwait(false);
        Init();
    }

    private void Init()
    {
        _playlistReference.PassPlaylist(ref _playList);
        _timedTask.Start(
                TimeSpan.FromMilliseconds(100),
                () => OnPropertyChanged(nameof(CurrentTime)));
        _globalHook.Register();
    }

    private async Task AsyncInit()
    {
        await Task.Run(async () => await _dataContext.Database.MigrateAsync());
        await Task.Run(async () =>
        {
            FontFamily = _settingsManager.Settings.FontFamily;
            WaveFormWidth = 480;
            WaveFormHeight = 70;
            IsUpdateExclamationMarkVisible = !await _versionChecker.IsLatestAsync();
            _waveFormDrawer.SetSize(WaveFormWidth, WaveFormHeight);
            await DrawPlaceholderLineAsync();
        });
        
        if (_settingsManager.Settings.ScanOnStartup)
        {
            await Task.Run(_folderScanner.ScanAllAsync);
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
        await DrawPlaceholderLineAsync();
        SelectedSong = notification.Audio;
        WaveForm = _mediaController.Bitmap;
        TotalTime = SelectedSong.Length.TotalMilliseconds;
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

    #region Commands
    [RelayCommand]
    private async Task QuickSearch()
    {
        _logger.Information("Searching for \'{SearchTerm}\'", SearchTerm);
        SearchResults.Clear();
        var results =
            string.IsNullOrEmpty(SearchTerm)
                ? await _audioRepository.ReadAsync()
                : await _audioRepository.ReadAsync(SearchTerm);
        SearchResults.AddRange(results);
    }

    [RelayCommand]
    private async Task JumpToSelecteSong()
    {
        if (SelectedIndex > -1)
        {
            await _mediaController.JumpToIndexAsync(SelectedIndex);
        }
    }

    [RelayCommand]
    private async Task PlayPause()
    {
        await _mediaController.PlayPauseAsync();
    }

    [RelayCommand]
    private void Stop()
    {
        _mediaController.Stop();
    }

    [RelayCommand]
    private async Task Next()
    {
        await _mediaController.NextAsync();
    }

    [RelayCommand]
    private async Task Previous()
    {
        await _mediaController.PreviousAsync();
    }

    [RelayCommand]
    private async Task Shuffle()
    {
        await _mediaController.Shuffle();
        SelectedIndex = 0;
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
    #endregion

    private async Task DrawPlaceholderLineAsync()
    {
        WaveForm = await _waveFormDrawer.LineAsync();
    }

    public async Task RefreshSoundWave()
    {
        _waveFormDrawer.SetSize(WaveFormWidth, WaveFormHeight);
        WaveForm = await _waveFormDrawer.WaveFormAsync(SelectedSong!.Path!);
    }

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