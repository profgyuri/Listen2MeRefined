﻿namespace Listen2MeRefined.Infrastructure.Mvvm;
using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;

public sealed partial class SettingsWindowViewModel : 
    ObservableObject,
    INotificationHandler<FolderBrowserNotification>
{
    private readonly ILogger _logger;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IRepository<MusicFolderModel> _musicFolderRepository;
    private readonly IRepository<PlaylistModel> _playlistRepository;
    private readonly IFolderScanner _folderScanner;
    private readonly IMediator _mediator;
    private readonly FontFamilies _installedFontFamilies;
    private readonly IVersionChecker _versionChecker;
    private readonly IFromFolderRemover _fromFolderRemover;

    private TimedTask? _timedTask;
    private int _secondsToCancelClear = 5;

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private string? _selectedFolder = "";
    [ObservableProperty] private string _selectedFontFamily = "";
    [ObservableProperty] private string _selectedNewSongWindowPosition = "";
    [ObservableProperty] private AudioOutputDevice? _selectedAudioOutputDevice;
    [ObservableProperty] private ObservableCollection<string> _folders = new();
    [ObservableProperty] private ObservableCollection<string> _fontFamilies = new();
    [ObservableProperty] private ObservableCollection<string> _newSongWindowPositions = new();
    [ObservableProperty] private ObservableCollection<AudioOutputDevice> _audioOutputDevices = new();
    [ObservableProperty] private bool _isClearMetadataButtonVisible = true;
    [ObservableProperty] private bool _isCancelClearMetadataButtonVisible;
    [ObservableProperty] private string _cancelClearMetadataButtonContent = "Cancel(5)";
    [ObservableProperty] private string _updateAvailableText = "";
    [ObservableProperty] private bool _isUpdateButtonVisible;
    
    public bool ScanOnStartup
    {
        get => _settingsManager.Settings.ScanOnStartup;
        set
        {
            _settingsManager.SaveSettings(s => s.ScanOnStartup = value);
            OnPropertyChanged();
        }
    }

    public bool DontScanOnStartup => !ScanOnStartup;

    public SettingsWindowViewModel(
        ILogger logger,
        ISettingsManager<AppSettings> settingsManager,
        IRepository<AudioModel> audioRepository,
        IMediator mediator,
        FontFamilies installedFontFamilies,
        IRepository<MusicFolderModel> musicFolderRepository,
        IRepository<PlaylistModel> playlistRepository,
        IFolderScanner folderScanner,
        IVersionChecker versionChecker,
        IFromFolderRemover fromFolderRemover)
    {
        _logger = logger;
        _settingsManager = settingsManager;
        _audioRepository = audioRepository;
        _mediator = mediator;
        _installedFontFamilies = installedFontFamilies;
        _musicFolderRepository = musicFolderRepository;
        _playlistRepository = playlistRepository;
        _folderScanner = folderScanner;
        _versionChecker = versionChecker;
        _fromFolderRemover = fromFolderRemover;

        Initialize().ConfigureAwait(false);
    }

    private async Task Initialize()
    {
        await Task.Run(async () =>
        {
            FontFamilies = new(_installedFontFamilies.FontFamilyNames);

            var settings = _settingsManager.Settings;
            Folders = new(settings.MusicFolders.Select(x => x.FullPath));
            FontFamily = settings.FontFamily;
            SelectedFontFamily = string.IsNullOrEmpty(settings.FontFamily) ? "Segoe UI" : settings.FontFamily;
            NewSongWindowPositions = new()
            {
                "Default",
                "Always on top",
                //todo: "Off"
            };
            SelectedNewSongWindowPosition = settings.NewSongWindowPosition;
            if (await _versionChecker.IsLatestAsync())
            {
                UpdateAvailableText = "You are using the latest version!";
                IsUpdateButtonVisible = false;
            }
            else
            {
                UpdateAvailableText = "Newer version is available!";
                IsUpdateButtonVisible = true;
            }
        });

        await GetAudioOutputDevices();
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedFontFamily));
        _settingsManager.SaveSettings(s => s.FontFamily = value);
        _mediator.Publish(new FontFamilyChangedNotification(value));
    }

    partial void OnSelectedAudioOutputDeviceChanged(AudioOutputDevice? value)
    {
        if (value is null)
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedAudioOutputDevice));
        _mediator.Publish(new AudioOutputDeviceChangedNotification(value));
    }

    partial void OnSelectedNewSongWindowPositionChanged(string value)
    {
        if (value is null)
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedNewSongWindowPosition));
        _settingsManager.SaveSettings(x => x.NewSongWindowPosition = value);
        _mediator.Publish(new NewSongWindowPositionChangedNotification(value));
    }

    #region Notification Handlers
    public async Task Handle(
        FolderBrowserNotification notification,
        CancellationToken cancellationToken)
    {
        var path = notification.Path;

        if (Folders.Contains(path))
        {
            return;
        }

        _logger.Debug("Adding path to music folders: {Path}", path);

        Folders.Add(path);

        _settingsManager.SaveSettings(s => s.MusicFolders = Folders.Select(x => new MusicFolderModel(x)).ToList());

        await _folderScanner.ScanAsync(path);
    }
    #endregion

    #region Commands
    [RelayCommand]
    private async Task RemoveFolder()
    {
        _logger.Debug("Removing folder: {Folder}", SelectedFolder);

        Folders.Remove(SelectedFolder!);
        await _fromFolderRemover.RemoveFromFolderAsync(SelectedFolder!);

        _settingsManager.SaveSettings(s => s.MusicFolders = Folders.Select(x => new MusicFolderModel(x)).ToList());
    }

    [RelayCommand]
    private void ClearMetadata()
    {
        _logger.Information("Clearing metadata...");

        _timedTask = new();
        _timedTask.Start(TimeSpan.FromSeconds(1), async () =>
        {
            if (_secondsToCancelClear == 0)
            {
                _logger.Verbose("Removing entries from database");
                await _audioRepository.RemoveAllAsync();
                await _musicFolderRepository.RemoveAllAsync();
                await _playlistRepository.RemoveAllAsync();
                _logger.Debug("Database was successfully cleared");
                
                Folders = new();

                await _timedTask?.StopAsync()!;
                IsClearMetadataButtonVisible = true;
                IsCancelClearMetadataButtonVisible = false;
                _secondsToCancelClear = 5;
                CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";

                _settingsManager.SaveSettings(x => x.MusicFolders = new());

                _logger.Debug("Metadata cleared");
            }

            _secondsToCancelClear--;
            CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";
        });
        IsClearMetadataButtonVisible = false;
        IsCancelClearMetadataButtonVisible = true;
    }

    [RelayCommand]
    private async Task CancelClearMetadataAsync()
    {
        _logger.Information("Clearing metadata canceled");

        await _timedTask?.StopAsync()!;
        IsClearMetadataButtonVisible = true;
        IsCancelClearMetadataButtonVisible = false;
        _secondsToCancelClear = 5;
        CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";
    }
    
    [RelayCommand]
    private async Task ForceScanAsync()
    {
        _logger.Information("Force scanning folders...");

        await _folderScanner.ScanAllAsync();
    }

    [RelayCommand]
    private async Task OpenBrowserForUpdate()
    {
        await Task.Run(_versionChecker.OpenUpdateLink);
    }
    #endregion

    private async Task GetAudioOutputDevices()
    {
        var devices = await Task.Run(() =>
        {
            var result = new List<AudioOutputDevice>();
            try
            {
                result = AudioDevices.GetOutputDevices().ToList();
            }
            catch (Exception ex)
            {
                _logger.Error("Could not enumerate output devices. " + ex.Message);
                if (ex.StackTrace is not null)
                {
                    _logger.Verbose(ex.StackTrace);
                }
            }

            return result;
        });
        foreach (var device in devices)
        {
            AudioOutputDevices.Add(device);
        }
        
        SelectedAudioOutputDevice = AudioOutputDevices[0];
    }
}