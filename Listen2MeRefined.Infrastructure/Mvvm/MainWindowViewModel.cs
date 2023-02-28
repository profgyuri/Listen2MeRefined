using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Source;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class MainWindowViewModel : 
    ObservableObject,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>
{
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

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string _searchTerm = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private ObservableCollection<AudioModel> _playList = new();
    [ObservableProperty] private SKBitmap _waveForm = new(1, 1);
    [ObservableProperty] private int _waveFormWidth;
    [ObservableProperty] private int _waveFormHeight;

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
        IWaveFormDrawer<SKBitmap> waveFormDrawer)
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

        Initialize().ConfigureAwait(false);
        
        _globalHook.Register();
    }

    private async Task Initialize()
    {
        _playlistReference.PassPlaylist(ref _playList);
        await Task.Run(async () => await _dataContext.Database.MigrateAsync());
        await Task.Run(async () =>
        {
            FontFamily = _settingsManager.Settings.FontFamily;
            WaveFormWidth = 470;
            WaveFormHeight = 70;
            _waveFormDrawer.SetSize(WaveFormWidth, WaveFormHeight);
            await DrawPlaceholderLineAsync();
        });
        
        if (_settingsManager.Settings.ScanOnStartup)
        {
            await Task.Run(async () => await _folderScanner.ScanAllAsync()).ConfigureAwait(false);
        }

        await Task.Run(() =>
        {
            _timedTask.Start(
                TimeSpan.FromMilliseconds(100),
                () => OnPropertyChanged(nameof(CurrentTime)));
        });
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
    private void Shuffle()
    {
        _mediaController.Shuffle();
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
}