using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Services.Contracts;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class SettingsWindowViewModel :
    ViewModelBase,
    INotificationHandler<FolderBrowserNotification>,
    INotificationHandler<PinnedFoldersChangedNotification>
{
    private const int MinCornerTriggerSizePx = 4;
    private const int MaxCornerTriggerSizePx = 64;
    private const int MinCornerDebounceMs = 5;
    private const int MaxCornerDebounceMs = 200;
    private const int MinStartupVolumePercent = 0;
    private const int MaxStartupVolumePercent = 100;

    private readonly ILogger _logger;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IRepository<MusicFolderModel> _musicFolderRepository;
    private readonly IRepository<PlaylistModel> _playlistRepository;
    private readonly IFolderScanner _folderScanner;
    private readonly IMediator _mediator;
    private readonly FontFamilies _installedFontFamilies;
    private readonly IFromFolderRemover _fromFolderRemover;
    private readonly IOutputDevice _outputDevice;
    private readonly IVersionChecker _versionChecker;
    private readonly IAppSettingsReadService _settingsReadService;
    private readonly IAppSettingsWriteService _settingsWriteService;
    private readonly IAppUpdateCheckService _appUpdateCheckService;
    private readonly IGlobalHookSettingsSyncService _globalHookSettingsSyncService;
    private readonly IPinnedFoldersService _pinnedFoldersService;
    private readonly IPlaybackDefaultsService _playbackDefaultsService;

    private TimedTask? _timedTask;
    private int _secondsToCancelClear = 5;
    private bool _isLoadingSettings;
    private bool _isSyncingGlobalHookState;

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
    [ObservableProperty] private string _cancelClearMetadataButtonContent = "Cancel (5)";
    [ObservableProperty] private string _updateAvailableText = "";
    [ObservableProperty] private bool _isUpdateButtonVisible;
    [ObservableProperty] private bool _enableGlobalMediaKeys;
    [ObservableProperty] private bool _enableCornerNowPlayingPopup;
    [ObservableProperty] private int _cornerTriggerSizePx = 10;
    [ObservableProperty] private int _cornerTriggerDebounceMs = 10;
    [ObservableProperty] private int _startupVolumePercent = 70;
    [ObservableProperty] private bool _startMuted;
    [ObservableProperty] private bool _autoCheckUpdatesOnStartup = true;
    [ObservableProperty] private bool _autoScanOnFolderAdd = true;
    [ObservableProperty] private bool _folderBrowserStartAtLastLocation = true;
    [ObservableProperty] private ObservableCollection<string> _pinnedFolders = new();
    [ObservableProperty] private string? _selectedPinnedFolder = "";

    public bool ScanOnStartup
    {
        get => _settingsReadService.GetScanOnStartup();
        set
        {
            _settingsWriteService.SetScanOnStartup(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DontScanOnStartup));
        }
    }

    public bool DontScanOnStartup => !ScanOnStartup;

    public SettingsWindowViewModel(
        ILogger logger,
        IRepository<AudioModel> audioRepository,
        IMediator mediator,
        FontFamilies installedFontFamilies,
        IRepository<MusicFolderModel> musicFolderRepository,
        IRepository<PlaylistModel> playlistRepository,
        IFolderScanner folderScanner,
        IFromFolderRemover fromFolderRemover,
        IOutputDevice outputDevice,
        IVersionChecker versionChecker,
        IAppSettingsReadService settingsReadService,
        IAppSettingsWriteService settingsWriteService,
        IAppUpdateCheckService appUpdateCheckService,
        IGlobalHookSettingsSyncService globalHookSettingsSyncService,
        IPinnedFoldersService pinnedFoldersService,
        IPlaybackDefaultsService playbackDefaultsService)
    {
        _logger = logger;
        _audioRepository = audioRepository;
        _mediator = mediator;
        _installedFontFamilies = installedFontFamilies;
        _musicFolderRepository = musicFolderRepository;
        _playlistRepository = playlistRepository;
        _folderScanner = folderScanner;
        _fromFolderRemover = fromFolderRemover;
        _outputDevice = outputDevice;
        _versionChecker = versionChecker;
        _settingsReadService = settingsReadService;
        _settingsWriteService = settingsWriteService;
        _appUpdateCheckService = appUpdateCheckService;
        _globalHookSettingsSyncService = globalHookSettingsSyncService;
        _pinnedFoldersService = pinnedFoldersService;
        _playbackDefaultsService = playbackDefaultsService;

        _logger.Debug("[SettingsWindowViewModel] initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        _isLoadingSettings = true;

        FontFamilies = new(_installedFontFamilies.FontFamilyNames);
        NewSongWindowPositions = new()
        {
            "Default",
            "Always on top"
        };

        Folders = new(_settingsReadService.GetMusicFolders());
        var selectedFont = _settingsReadService.GetFontFamily();
        FontFamily = selectedFont;
        SelectedFontFamily = string.IsNullOrEmpty(selectedFont) ? "Segoe UI" : selectedFont;

        var selectedWindowPosition = _settingsReadService.GetNewSongWindowPosition();
        SelectedNewSongWindowPosition = string.IsNullOrWhiteSpace(selectedWindowPosition)
            ? "Default"
            : selectedWindowPosition;
        EnableGlobalMediaKeys = _settingsReadService.GetEnableGlobalMediaKeys();
        EnableCornerNowPlayingPopup = _settingsReadService.GetEnableCornerNowPlayingPopup();
        CornerTriggerSizePx = Math.Clamp((int)_settingsReadService.GetCornerTriggerSizePx(), MinCornerTriggerSizePx, MaxCornerTriggerSizePx);
        CornerTriggerDebounceMs = Math.Clamp((int)_settingsReadService.GetCornerTriggerDebounceMs(), MinCornerDebounceMs, MaxCornerDebounceMs);
        StartupVolumePercent = _playbackDefaultsService.ToVolumePercent(_settingsReadService.GetStartupVolume());
        StartMuted = _settingsReadService.GetStartMuted();
        AutoCheckUpdatesOnStartup = _settingsReadService.GetAutoCheckUpdatesOnStartup();
        AutoScanOnFolderAdd = _settingsReadService.GetAutoScanOnFolderAdd();
        FolderBrowserStartAtLastLocation = _settingsReadService.GetFolderBrowserStartAtLastLocation();
        ReloadPinnedFolders();

        await GetAudioOutputDevices();
        _isLoadingSettings = false;

        if (AutoCheckUpdatesOnStartup)
        {
            await CheckForUpdatesAsync();
        }
        else
        {
            UpdateAvailableText = "Automatic update checks are disabled.";
            IsUpdateButtonVisible = false;
        }

        _logger.Debug("[SettingsWindowViewModel] Finished InitializeCoreAsync");
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Font family changed to: {FontFamily}", value);
        _settingsWriteService.SetFontFamily(value);
        _ = _mediator.Publish(new FontFamilyChangedNotification(value));
    }

    partial void OnSelectedAudioOutputDeviceChanged(AudioOutputDevice? value)
    {
        if (_isLoadingSettings || value is null)
        {
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Audio output device changed to: {DeviceName}", value.Name);
        _settingsWriteService.SetAudioOutputDeviceName(value.Name);
        _ = _mediator.Publish(new AudioOutputDeviceChangedNotification(value));
    }

    partial void OnSelectedNewSongWindowPositionChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _logger.Information("[SettingsWindowViewModel] New song window position changed to: {Position}", value);
        _settingsWriteService.SetNewSongWindowPosition(value);
        _ = _mediator.Publish(new NewSongWindowPositionChangedNotification(value));
    }

    partial void OnEnableGlobalMediaKeysChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetEnableGlobalMediaKeys(value);
        _ = SyncGlobalHookRegistrationAsync();
    }

    partial void OnEnableCornerNowPlayingPopupChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetEnableCornerNowPlayingPopup(value);
        _ = SyncGlobalHookRegistrationAsync();
    }

    partial void OnCornerTriggerSizePxChanged(int value)
    {
        var clamped = Math.Clamp(value, MinCornerTriggerSizePx, MaxCornerTriggerSizePx);
        if (clamped != value)
        {
            CornerTriggerSizePx = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetCornerTriggerSizePx((short)clamped);
    }

    partial void OnCornerTriggerDebounceMsChanged(int value)
    {
        var clamped = Math.Clamp(value, MinCornerDebounceMs, MaxCornerDebounceMs);
        if (clamped != value)
        {
            CornerTriggerDebounceMs = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetCornerTriggerDebounceMs((short)clamped);
    }

    partial void OnStartupVolumePercentChanged(int value)
    {
        var clamped = Math.Clamp(value, MinStartupVolumePercent, MaxStartupVolumePercent);
        if (clamped != value)
        {
            StartupVolumePercent = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetStartupVolume(_playbackDefaultsService.FromVolumePercent(clamped));
        if (clamped > 0 && StartMuted)
        {
            StartMuted = false;
        }
    }

    partial void OnStartMutedChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetStartMuted(value);
    }

    partial void OnAutoCheckUpdatesOnStartupChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetAutoCheckUpdatesOnStartup(value);
        if (!value)
        {
            UpdateAvailableText = "Automatic update checks are disabled.";
            IsUpdateButtonVisible = false;
        }
    }

    partial void OnAutoScanOnFolderAddChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetAutoScanOnFolderAdd(value);
    }

    partial void OnFolderBrowserStartAtLastLocationChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriteService.SetFolderBrowserStartAtLastLocation(value);
    }

    [RelayCommand]
    private async Task RemoveFolder()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolder))
        {
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Removing folder: {Folder}", SelectedFolder);
        Folders.Remove(SelectedFolder);
        await _fromFolderRemover.RemoveFromFolderAsync(SelectedFolder);
        _settingsWriteService.SetMusicFolders(Folders);
        _logger.Verbose("[SettingsWindowViewModel] Folder removed: {Folder}", SelectedFolder);
    }

    [RelayCommand]
    private void RemovePinnedFolder()
    {
        if (string.IsNullOrWhiteSpace(SelectedPinnedFolder))
        {
            return;
        }

        PinnedFolders.Remove(SelectedPinnedFolder);
        PersistPinnedFolders();
    }

    [RelayCommand]
    private void ClearInvalidPins()
    {
        var cleanedPinnedFolders = _pinnedFoldersService.NormalizeExisting(PinnedFolders);

        PinnedFolders.Clear();
        foreach (var pinnedFolder in cleanedPinnedFolders)
        {
            PinnedFolders.Add(pinnedFolder);
        }

        PersistPinnedFolders();
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

                _settingsWriteService.SetMusicFolders([]);
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
    private async Task CheckForUpdatesNowAsync()
    {
        await CheckForUpdatesAsync();
    }

    [RelayCommand]
    private async Task OpenBrowserForUpdate()
    {
        _logger.Information("[SettingsWindowViewModel] Opening browser to get update...");
        await Task.Run(_versionChecker.OpenUpdateLink);
    }

    private async Task CheckForUpdatesAsync()
    {
        var status = await _appUpdateCheckService.CheckForUpdatesAsync();
        UpdateAvailableText = status.Message;
        IsUpdateButtonVisible = status.CanOpenUpdateLink;
    }

    private async Task GetAudioOutputDevices()
    {
        AudioOutputDevices.Clear();

        var devices = await Task.Run(() =>
        {
            var result = Enumerable.Empty<AudioOutputDevice>();
            try
            {
                result = _outputDevice.EnumerateOutputDevices();
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

        if (AudioOutputDevices.Count == 0)
        {
            return;
        }

        var selectedIndex = 0;
        var savedName = _settingsReadService.GetAudioOutputDeviceName();
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

    private void PersistPinnedFolders()
    {
        _settingsWriteService.SetPinnedFolders(_pinnedFoldersService.Normalize(PinnedFolders));
    }

    private void ReloadPinnedFolders()
    {
        var pinnedFolders = _pinnedFoldersService.Normalize(_settingsReadService.GetPinnedFolders());

        PinnedFolders.Clear();
        foreach (var pinnedFolder in pinnedFolders)
        {
            PinnedFolders.Add(pinnedFolder);
        }
    }

    private async Task SyncGlobalHookRegistrationAsync()
    {
        if (_isSyncingGlobalHookState)
        {
            return;
        }

        _isSyncingGlobalHookState = true;
        try
        {
            await _globalHookSettingsSyncService.SyncAsync(EnableGlobalMediaKeys, EnableCornerNowPlayingPopup);
        }
        finally
        {
            _isSyncingGlobalHookState = false;
        }
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
        _settingsWriteService.SetMusicFolders(Folders);

        if (!AutoScanOnFolderAdd)
        {
            _logger.Information("[SettingsWindowViewModel] Auto-scan on folder add is disabled.");
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Starting folder scan for path: {Path}", path);
        await _folderScanner.ScanAsync(path);
        _logger.Verbose("[SettingsWindowViewModel] Path was scanned and added to music folders: {Path}", path);
    }

    public async Task Handle(
        PinnedFoldersChangedNotification notification,
        CancellationToken cancellationToken)
    {
        var pinnedFolders = _pinnedFoldersService.Normalize(notification.PinnedFolders);
        PinnedFolders = new ObservableCollection<string>(pinnedFolders);
        _settingsWriteService.SetPinnedFolders(pinnedFolders);

        await Task.CompletedTask;
    }
}
