namespace Listen2MeRefined.Infrastructure.Mvvm;
using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;

public sealed partial class SettingsWindowViewModel : 
    ViewModelBase,
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

        _logger.Debug("[SettingsWindowViewModel] initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        FontFamilies = new(_installedFontFamilies.FontFamilyNames);

        var settings = _settingsManager.Settings;
        Folders = new(settings.MusicFolders.Select(x => x.FullPath));
        FontFamily = settings.FontFamily;
        SelectedFontFamily = string.IsNullOrEmpty(settings.FontFamily)
            ? "Segoe UI"
            : settings.FontFamily;

        NewSongWindowPositions = new()
        {
            "Default",
            "Always on top",
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

        await GetAudioOutputDevices();
        _logger.Debug("[SettingsWindowViewModel] Finished InitializeCoreAsync");
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        _logger.Information("[SettingsWindowViewModel] Font family changed to: {FontFamily}", value);
        OnPropertyChanged(nameof(SelectedFontFamily));
        _settingsManager.SaveSettings(s => s.FontFamily = value);
        _mediator.Publish(new FontFamilyChangedNotification(value));
    }

    partial void OnSelectedAudioOutputDeviceChanged(AudioOutputDevice? value)
    {
        if (value is null)
        {
            _logger.Error("[SettingsWindowViewModel] Selected audio output device is null. This should not happen.");
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Audio output device changed to: {DeviceName}", value.Name);
        OnPropertyChanged(nameof(SelectedAudioOutputDevice));
        _settingsManager.SaveSettings(x => x.AudioOutputDeviceName = value.Name);
        _mediator.Publish(new AudioOutputDeviceChangedNotification(value));
    }

    partial void OnSelectedNewSongWindowPositionChanged(string value)
    {
        if (value is null)
        {
            _logger.Error("[SettingsWindowViewModel] Selected new song window position is null. This should not happen.");
            return;
        }

        _logger.Information("[SettingsWindowViewModel] New song window position changed to: {Position}", value);
        OnPropertyChanged(nameof(SelectedNewSongWindowPosition));
        _settingsManager.SaveSettings(x => x.NewSongWindowPosition = value);
        _mediator.Publish(new NewSongWindowPositionChangedNotification(value));
    }

    public async Task Handle(
        FolderBrowserNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.Information("[SettingsWindowViewModel] Received FolderBrowserNotification with path: {Path}", notification.Path);
        var path = notification.Path;

        if (Folders.Contains(path))
        {
            _logger.Debug("[SettingsWindowViewModel] Path is already in music folders: {Path}", path);
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Adding path to music folders: {Path}", path);

        Folders.Add(path);

        _settingsManager.SaveSettings(s => s.MusicFolders = Folders.Select(x => new MusicFolderModel(x)).ToList());

        _logger.Information("[SettingsWindowViewModel] Starting folder scan for path: {Path}", path);
        await _folderScanner.ScanAsync(path);

        _logger.Verbose("[SettingsWindowViewModel] Path was scanned and added to music folders: {Path}", path);
    }
    [RelayCommand]
    private async Task RemoveFolder()
    {
        _logger.Information("[SettingsWindowViewModel] Removing folder: {Folder}", SelectedFolder);

        Folders.Remove(SelectedFolder!);
        await _fromFolderRemover.RemoveFromFolderAsync(SelectedFolder!);

        _settingsManager.SaveSettings(s => s.MusicFolders = Folders.Select(x => new MusicFolderModel(x)).ToList());
        _logger.Verbose("[SettingsWindowViewModel] Folder removed: {Folder}", SelectedFolder);
    }

    [RelayCommand]
    private void ClearMetadata()
    {
        _logger.Information("[SettingsWindowViewModel] Clearing metadata...");

        _timedTask = new();
        _timedTask.Start(TimeSpan.FromSeconds(1), async () =>
        {
            if (_secondsToCancelClear == 0)
            {
                _logger.Verbose("[SettingsWindowViewModel] Removing entries from database...");
                await _audioRepository.RemoveAllAsync();
                await _musicFolderRepository.RemoveAllAsync();
                await _playlistRepository.RemoveAllAsync();
                _logger.Debug("[SettingsWindowViewModel] Database was successfully cleared");
                
                Folders = new();

                await _timedTask?.StopAsync()!;
                IsClearMetadataButtonVisible = true;
                IsCancelClearMetadataButtonVisible = false;
                _secondsToCancelClear = 5;
                CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";

                _settingsManager.SaveSettings(x => x.MusicFolders = new());

                _logger.Debug("[SettingsWindowViewModel] Metadata cleared");
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
        _logger.Information("[SettingsWindowViewModel] Clearing metadata canceled");

        await _timedTask?.StopAsync()!;
        IsClearMetadataButtonVisible = true;
        IsCancelClearMetadataButtonVisible = false;
        _secondsToCancelClear = 5;
        CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";
    }
    
    [RelayCommand]
    private async Task ForceScanAsync()
    {
        _logger.Information("[SettingsWindowViewModel] Force scanning folders...");

        await _folderScanner.ScanAllAsync();
    }

    [RelayCommand]
    private async Task OpenBrowserForUpdate()
    {
        _logger.Information("[SettingsWindowViewModel] Opening browser to get update...");
        await Task.Run(_versionChecker.OpenUpdateLink);
    }

    private async Task GetAudioOutputDevices()
    {
        var devices = await Task.Run(() =>
        {
            var result = Enumerable.Empty<AudioOutputDevice>();
            try
            {
                result = AudioDevices.GetOutputDevices();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[SettingsWindowViewModel] Could not enumerate output devices");
            }

            return result;
        });
        foreach (var device in devices)
        {
            AudioOutputDevices.Add(device);
        }
        
        var selectedIndex = 0; // The first element is "Windows Default"
        var savedName = _settingsManager.Settings.AudioOutputDeviceName;
        if (!string.IsNullOrEmpty(savedName))
        {
            var audioOutputDevice = AudioOutputDevices
                .FirstOrDefault(x => x.Name.Equals(savedName, StringComparison.OrdinalIgnoreCase));
            if (audioOutputDevice is not null)
            {
                selectedIndex = AudioOutputDevices.IndexOf(audioOutputDevice);
            }
        }

        SelectedAudioOutputDevice = AudioOutputDevices[selectedIndex];
    }
}