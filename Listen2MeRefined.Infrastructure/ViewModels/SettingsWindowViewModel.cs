using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Scanning;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.Infrastructure.Settings.Playback;

namespace Listen2MeRefined.Infrastructure.ViewModels;

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
    private const int MinTaskPercentageStep = 1;
    private const int MaxTaskPercentageStep = 25;
    private const int MinScanMilestoneInterval = 5;
    private const int MaxScanMilestoneInterval = 500;

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
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IAppUpdateChecker _appUpdateChecker;
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;
    private readonly IGlobalHookSettingsSyncService _globalHookSettingsSyncService;
    private readonly IPinnedFoldersService _pinnedFoldersService;
    private readonly IPlaybackDefaultsService _playbackDefaultsService;
    private readonly IPlaylistLibraryService _playlistLibraryService;

    private TimedTask? _timedTask;
    private int _secondsToCancelClear = 5;
    private bool _isLoadingSettings;
    private bool _isSyncingGlobalHookState;
    private bool _isUpdatingFolderSelection;
    private readonly Dictionary<string, bool> _folderRecursionByPath = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private string? _selectedFolder = "";
    [ObservableProperty] private bool _selectedFolderIncludeSubdirectories;
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
    [ObservableProperty] private bool _showTaskPercentage = true;
    [ObservableProperty] private int _taskPercentageReportInterval = 1;
    [ObservableProperty] private bool _showScanMilestoneCount;
    [ObservableProperty] private int _scanMilestoneInterval = 25;
    [ObservableProperty] private TaskStatusCountBasis _selectedScanMilestoneBasis = TaskStatusCountBasis.Processed;
    [ObservableProperty] private bool _folderBrowserStartAtLastLocation = true;
    [ObservableProperty] private ObservableCollection<string> _pinnedFolders = new();
    [ObservableProperty] private string? _selectedPinnedFolder = "";
    [ObservableProperty] private ObservableCollection<PlaylistSummary> _playlists = new();
    [ObservableProperty] private PlaylistSummary? _selectedPlaylist;
    [ObservableProperty] private string _playlistNameInput = "";
    [ObservableProperty] private SearchResultsTransferMode _selectedSearchResultsTransferMode = SearchResultsTransferMode.Move;

    public ObservableCollection<TaskStatusCountBasis> ScanMilestoneBases { get; } =
        new(Enum.GetValues<TaskStatusCountBasis>());
    public ObservableCollection<SearchResultsTransferMode> SearchResultsTransferModes { get; } =
        new(Enum.GetValues<SearchResultsTransferMode>());

    public bool ScanOnStartup
    {
        get => _settingsReader.GetScanOnStartup();
        set
        {
            _settingsWriter.SetScanOnStartup(value);
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
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IAppUpdateChecker appUpdateChecker,
        IBackgroundTaskStatusService backgroundTaskStatusService,
        IGlobalHookSettingsSyncService globalHookSettingsSyncService,
        IPinnedFoldersService pinnedFoldersService,
        IPlaybackDefaultsService playbackDefaultsService,
        IPlaylistLibraryService playlistLibraryService)
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
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _appUpdateChecker = appUpdateChecker;
        _backgroundTaskStatusService = backgroundTaskStatusService;
        _globalHookSettingsSyncService = globalHookSettingsSyncService;
        _pinnedFoldersService = pinnedFoldersService;
        _playbackDefaultsService = playbackDefaultsService;
        _playlistLibraryService = playlistLibraryService;

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

        var musicFolderRequests = _settingsReader.GetMusicFolderRequests();
        _folderRecursionByPath.Clear();
        foreach (var folderRequest in musicFolderRequests)
        {
            _folderRecursionByPath[folderRequest.Path] = folderRequest.IncludeSubdirectories;
        }

        Folders = new(musicFolderRequests.Select(x => x.Path));
        var selectedFont = _settingsReader.GetFontFamily();
        FontFamily = selectedFont;
        SelectedFontFamily = string.IsNullOrEmpty(selectedFont) ? "Segoe UI" : selectedFont;

        var selectedWindowPosition = _settingsReader.GetNewSongWindowPosition();
        SelectedNewSongWindowPosition = string.IsNullOrWhiteSpace(selectedWindowPosition)
            ? "Default"
            : selectedWindowPosition;
        EnableGlobalMediaKeys = _settingsReader.GetEnableGlobalMediaKeys();
        EnableCornerNowPlayingPopup = _settingsReader.GetEnableCornerNowPlayingPopup();
        CornerTriggerSizePx = Math.Clamp((int)_settingsReader.GetCornerTriggerSizePx(), MinCornerTriggerSizePx, MaxCornerTriggerSizePx);
        CornerTriggerDebounceMs = Math.Clamp((int)_settingsReader.GetCornerTriggerDebounceMs(), MinCornerDebounceMs, MaxCornerDebounceMs);
        StartupVolumePercent = _playbackDefaultsService.ToVolumePercent(_settingsReader.GetStartupVolume());
        StartMuted = _settingsReader.GetStartMuted();
        AutoCheckUpdatesOnStartup = _settingsReader.GetAutoCheckUpdatesOnStartup();
        AutoScanOnFolderAdd = _settingsReader.GetAutoScanOnFolderAdd();
        ShowTaskPercentage = _settingsReader.GetShowTaskPercentage();
        TaskPercentageReportInterval = Math.Clamp(
            (int)_settingsReader.GetTaskPercentageReportInterval(),
            MinTaskPercentageStep,
            MaxTaskPercentageStep);
        ShowScanMilestoneCount = _settingsReader.GetShowScanMilestoneCount();
        ScanMilestoneInterval = Math.Clamp(
            (int)_settingsReader.GetScanMilestoneInterval(),
            MinScanMilestoneInterval,
            MaxScanMilestoneInterval);
        SelectedScanMilestoneBasis = _settingsReader.GetScanMilestoneBasis();
        FolderBrowserStartAtLastLocation = _settingsReader.GetFolderBrowserStartAtLastLocation();
        SelectedSearchResultsTransferMode = _settingsReader.GetSearchResultsTransferMode();
        ReloadPinnedFolders();
        await ReloadPlaylistsAsync(ct);

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
        _settingsWriter.SetFontFamily(value);
        _ = _mediator.Publish(new FontFamilyChangedNotification(value));
    }

    partial void OnSelectedAudioOutputDeviceChanged(AudioOutputDevice? value)
    {
        if (_isLoadingSettings || value is null)
        {
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Audio output device changed to: {DeviceName}", value.Name);
        _settingsWriter.SetAudioOutputDeviceName(value.Name);
        _ = _mediator.Publish(new AudioOutputDeviceChangedNotification(value));
    }

    partial void OnSelectedNewSongWindowPositionChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _logger.Information("[SettingsWindowViewModel] New song window position changed to: {Position}", value);
        _settingsWriter.SetNewSongWindowPosition(value);
        _ = _mediator.Publish(new NewSongWindowPositionChangedNotification(value));
    }

    partial void OnSelectedFolderChanged(string? value)
    {
        _isUpdatingFolderSelection = true;
        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedFolderIncludeSubdirectories = false;
                return;
            }

            SelectedFolderIncludeSubdirectories =
                _folderRecursionByPath.TryGetValue(value, out var includeSubdirectories)
                && includeSubdirectories;
        }
        finally
        {
            _isUpdatingFolderSelection = false;
        }
    }

    partial void OnSelectedFolderIncludeSubdirectoriesChanged(bool value)
    {
        if (_isLoadingSettings || _isUpdatingFolderSelection || string.IsNullOrWhiteSpace(SelectedFolder))
        {
            return;
        }

        _folderRecursionByPath[SelectedFolder] = value;
        _settingsWriter.SetFolderIncludeSubdirectories(SelectedFolder, value);
    }

    partial void OnEnableGlobalMediaKeysChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetEnableGlobalMediaKeys(value);
        _ = SyncGlobalHookRegistrationAsync();
    }

    partial void OnEnableCornerNowPlayingPopupChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetEnableCornerNowPlayingPopup(value);
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

        _settingsWriter.SetCornerTriggerSizePx((short)clamped);
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

        _settingsWriter.SetCornerTriggerDebounceMs((short)clamped);
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

        _settingsWriter.SetStartupVolume(_playbackDefaultsService.FromVolumePercent(clamped));
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

        _settingsWriter.SetStartMuted(value);
    }

    partial void OnAutoCheckUpdatesOnStartupChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetAutoCheckUpdatesOnStartup(value);
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

        _settingsWriter.SetAutoScanOnFolderAdd(value);
    }

    partial void OnShowTaskPercentageChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetShowTaskPercentage(value);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnTaskPercentageReportIntervalChanged(int value)
    {
        var clamped = Math.Clamp(value, MinTaskPercentageStep, MaxTaskPercentageStep);
        if (clamped != value)
        {
            TaskPercentageReportInterval = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetTaskPercentageReportInterval((short)clamped);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnShowScanMilestoneCountChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetShowScanMilestoneCount(value);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnScanMilestoneIntervalChanged(int value)
    {
        var clamped = Math.Clamp(value, MinScanMilestoneInterval, MaxScanMilestoneInterval);
        if (clamped != value)
        {
            ScanMilestoneInterval = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetScanMilestoneInterval((short)clamped);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnSelectedScanMilestoneBasisChanged(TaskStatusCountBasis value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetScanMilestoneBasis(value);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnFolderBrowserStartAtLastLocationChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetFolderBrowserStartAtLastLocation(value);
    }

    partial void OnSelectedSearchResultsTransferModeChanged(SearchResultsTransferMode value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetSearchResultsTransferMode(value);
    }

    [RelayCommand]
    private async Task RemoveFolder()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolder))
        {
            return;
        }

        _logger.Information<string>("[SettingsWindowViewModel] Removing folder: {Folder}", SelectedFolder);
        Folders.Remove(SelectedFolder);
        _folderRecursionByPath.Remove(SelectedFolder);
        await _fromFolderRemover.RemoveFromFolderAsync(SelectedFolder);
        PersistMusicFolders();
        _logger.Verbose<string>("[SettingsWindowViewModel] Folder removed: {Folder}", SelectedFolder);
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
    private async Task CreatePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistNameInput))
        {
            return;
        }

        try
        {
            var created = await _playlistLibraryService.CreatePlaylistAsync(PlaylistNameInput.Trim());
            await ReloadPlaylistsAsync();
            SelectedPlaylist = Playlists.FirstOrDefault(x => x.Id == created.Id);
            PlaylistNameInput = string.Empty;
            await _mediator.Publish(new PlaylistCreatedNotification(created.Id, created.Name));
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[SettingsWindowViewModel] Could not create playlist");
        }
    }

    [RelayCommand]
    private async Task RenameSelectedPlaylistAsync()
    {
        if (SelectedPlaylist is null || string.IsNullOrWhiteSpace(PlaylistNameInput))
        {
            return;
        }

        var playlistId = SelectedPlaylist.Id;
        var newName = PlaylistNameInput.Trim();
        try
        {
            await _playlistLibraryService.RenamePlaylistAsync(playlistId, newName);
            await ReloadPlaylistsAsync();
            SelectedPlaylist = Playlists.FirstOrDefault(x => x.Id == playlistId);
            await _mediator.Publish(new PlaylistRenamedNotification(playlistId, newName));
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[SettingsWindowViewModel] Could not rename playlist");
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedPlaylistAsync()
    {
        if (SelectedPlaylist is null)
        {
            return;
        }

        var playlistId = SelectedPlaylist.Id;
        try
        {
            await _playlistLibraryService.DeletePlaylistAsync(playlistId);
            await ReloadPlaylistsAsync();
            SelectedPlaylist = Playlists.FirstOrDefault();
            PlaylistNameInput = SelectedPlaylist?.Name ?? string.Empty;
            await _mediator.Publish(new PlaylistDeletedNotification(playlistId));
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[SettingsWindowViewModel] Could not delete playlist");
        }
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
                Playlists = new();
                SelectedPlaylist = null;
                PlaylistNameInput = string.Empty;

                await _timedTask?.StopAsync()!;
                IsClearMetadataButtonVisible = true;
                IsCancelClearMetadataButtonVisible = false;
                _secondsToCancelClear = 5;
                CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";

                _folderRecursionByPath.Clear();
                _settingsWriter.SetMusicFolders(Array.Empty<FolderScanRequest>());
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
        await _folderScanner.ScanAllAsync(ScanMode.FullRefresh);
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
        var status = await _appUpdateChecker.CheckForUpdatesAsync();
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
        var savedName = _settingsReader.GetAudioOutputDeviceName();
        if (!string.IsNullOrEmpty(savedName))
        {
            var audioOutputDevice = Enumerable
                .FirstOrDefault<AudioOutputDevice>(AudioOutputDevices, x => x.Name.Equals(savedName, StringComparison.OrdinalIgnoreCase));
            if (audioOutputDevice is not null)
            {
                selectedIndex = AudioOutputDevices.IndexOf(audioOutputDevice);
            }
        }

        SelectedAudioOutputDevice = AudioOutputDevices[selectedIndex];
    }

    private void PersistMusicFolders()
    {
        var folders = Enumerable
            .Select<string, FolderScanRequest>(Folders, path => new FolderScanRequest(
                path,
                _folderRecursionByPath.TryGetValue(path, out var includeSubdirectories) && includeSubdirectories));
        _settingsWriter.SetMusicFolders(folders);
    }

    private void PersistPinnedFolders()
    {
        _settingsWriter.SetPinnedFolders(_pinnedFoldersService.Normalize(PinnedFolders));
    }

    private void ReloadPinnedFolders()
    {
        var pinnedFolders = _pinnedFoldersService.Normalize(_settingsReader.GetPinnedFolders());

        PinnedFolders.Clear();
        foreach (var pinnedFolder in pinnedFolders)
        {
            PinnedFolders.Add(pinnedFolder);
        }
    }

    private async Task ReloadPlaylistsAsync(CancellationToken ct = default)
    {
        var playlists = await _playlistLibraryService.GetAllPlaylistsAsync(ct);
        Playlists = new ObservableCollection<PlaylistSummary>(playlists);

        if (SelectedPlaylist is not null)
        {
            SelectedPlaylist = Playlists.FirstOrDefault(x => x.Id == SelectedPlaylist.Id);
        }
        else if (Playlists.Count > 0)
        {
            SelectedPlaylist = Playlists[0];
        }

        PlaylistNameInput = SelectedPlaylist?.Name ?? string.Empty;
    }

    partial void OnSelectedPlaylistChanged(PlaylistSummary? value)
    {
        PlaylistNameInput = value?.Name ?? string.Empty;
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
        _folderRecursionByPath[path] = false;
        PersistMusicFolders();

        if (!AutoScanOnFolderAdd)
        {
            _logger.Information("[SettingsWindowViewModel] Auto-scan on folder add is disabled.");
            return;
        }

        _logger.Information("[SettingsWindowViewModel] Starting folder scan for path: {Path}", path);
        _ = Task.Run(async () =>
        {
            try
            {
                await _folderScanner.ScanAsync(path, ScanMode.Incremental, CancellationToken.None).ConfigureAwait(false);
                _logger.Verbose("[SettingsWindowViewModel] Path was scanned and added to music folders: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[SettingsWindowViewModel] Failed to scan newly added path: {Path}", path);
            }
        }, CancellationToken.None);
    }

    public async Task Handle(
        PinnedFoldersChangedNotification notification,
        CancellationToken cancellationToken)
    {
        var pinnedFolders = _pinnedFoldersService.Normalize(notification.PinnedFolders);
        PinnedFolders = new ObservableCollection<string>(pinnedFolders);
        _settingsWriter.SetPinnedFolders(pinnedFolders);

        await Task.CompletedTask;
    }
}
