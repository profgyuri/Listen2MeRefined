using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Scanning.Folders;

namespace Listen2MeRefined.Infrastructure.Settings;

public sealed class AppSettingsReader : IAppSettingsReader
{
    private readonly ISettingsManager<AppSettings> _settingsManager;

    public AppSettingsReader(ISettingsManager<AppSettings> settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public string GetFontFamily() => _settingsManager.Settings.FontFamily;
    public string GetNewSongWindowPosition() => _settingsManager.Settings.NewSongWindowPosition;
    public string GetAudioOutputDeviceName() => _settingsManager.Settings.AudioOutputDeviceName;
    public IReadOnlyList<string> GetMusicFolders() => _settingsManager.Settings.MusicFolders.Select(x => x.FullPath).ToList();
    public IReadOnlyList<FolderScanRequest> GetMusicFolderRequests() =>
        _settingsManager.Settings.MusicFolders
            .Select(x => new FolderScanRequest(x.FullPath, x.IncludeSubdirectories))
            .ToList();
    public bool GetScanOnStartup() => _settingsManager.Settings.ScanOnStartup;
    public bool GetEnableGlobalMediaKeys() => _settingsManager.Settings.EnableGlobalMediaKeys;
    public bool GetEnableCornerNowPlayingPopup() => _settingsManager.Settings.EnableCornerNowPlayingPopup;
    public short GetCornerTriggerSizePx() => _settingsManager.Settings.CornerTriggerSizePx;
    public short GetCornerTriggerDebounceMs() => _settingsManager.Settings.CornerTriggerDebounceMs;
    public float GetStartupVolume() => _settingsManager.Settings.StartupVolume;
    public bool GetStartMuted() => _settingsManager.Settings.StartMuted;
    public bool GetAutoCheckUpdatesOnStartup() => _settingsManager.Settings.AutoCheckUpdatesOnStartup;
    public bool GetUseCompactPlaylistView() => _settingsManager.Settings.UseCompactPlaylistView;
    public bool GetAutoScanOnFolderAdd() => _settingsManager.Settings.AutoScanOnFolderAdd;
    public bool GetShowTaskPercentage() => _settingsManager.Settings.ShowTaskPercentage;
    public short GetTaskPercentageReportInterval() => _settingsManager.Settings.TaskPercentageReportInterval;
    public bool GetShowScanMilestoneCount() => _settingsManager.Settings.ShowScanMilestoneCount;
    public short GetScanMilestoneInterval() => _settingsManager.Settings.ScanMilestoneInterval;
    public TaskStatusCountBasis GetScanMilestoneBasis() => _settingsManager.Settings.ScanMilestoneBasis;
    public bool GetFolderBrowserStartAtLastLocation() => _settingsManager.Settings.FolderBrowserStartAtLastLocation;
    public string GetLastBrowsedFolder() => _settingsManager.Settings.LastBrowsedFolder;
    public IReadOnlyList<string> GetPinnedFolders() => _settingsManager.Settings.PinnedFolders.ToList();
}
