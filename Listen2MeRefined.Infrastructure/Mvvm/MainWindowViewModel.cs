using System.Collections.ObjectModel;
using System.Text;
using Dapper;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using SkiaSharp;
using Source;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class MainWindowViewModel : 
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchNotification>
{
    private readonly ILogger _logger;
    private readonly IMediaController<SKBitmap> _mediaController;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IAdvancedDataReader<ParameterizedQuery, AudioModel> _advancedAudioReader;
    private readonly IGlobalHook _globalHook;
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
        _audioRepository = audioRepository;
        _advancedAudioReader = advancedAudioReader;
        _globalHook = globalHook;
        _waveFormDrawer = waveFormDrawer;

        dataContext.Database.Migrate();

        if (settingsManager.Settings.ScanOnStartup)
        {
            Task.Run(async () => await folderScanner.ScanAllAsync()).ConfigureAwait(false);
        }

        _fontFamily = settingsManager.Settings.FontFamily;

        playlistReference.PassPlaylist(ref _playList);
        timedTask.Start(
            TimeSpan.FromMilliseconds(100),
            () => OnPropertyChanged(nameof(CurrentTime)));
        _globalHook.Register();
        
        WaveFormWidth = 470;
        WaveFormHeight = 70;
        _waveFormDrawer.SetSize(WaveFormWidth, WaveFormHeight);
        DrawPlaceholderLineAsync().ConfigureAwait(false);
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
        _searchResults.Clear();
        _searchResults.AddRange(result);
    }
    #endregion

    #region Commands
    [RelayCommand]
    private async Task QuickSearch()
    {
        _logger.Information("Searching for \'{SearchTerm}\'", _searchTerm);
        _searchResults.Clear();
        var results =
            string.IsNullOrEmpty(_searchTerm)
                ? await _audioRepository.ReadAsync()
                : await _audioRepository.ReadAsync(_searchTerm);
        _searchResults.AddRange(results);
    }

    [RelayCommand]
    private async Task JumpToSelecteSong()
    {
        if (_selectedIndex > -1)
        {
            await _mediaController.JumpToIndexAsync(_selectedIndex);
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
        WaveForm = await _waveFormDrawer.WaveFormAsync(SelectedSong.Path);
    }
}