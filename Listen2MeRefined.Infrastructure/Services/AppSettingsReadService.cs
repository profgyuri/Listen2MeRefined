using Listen2MeRefined.Infrastructure.Storage;

namespace Listen2MeRefined.Infrastructure.Services;

using Contracts;

public sealed class AppSettingsReadService : IAppSettingsReadService
{
    private readonly ISettingsManager<AppSettings> _settingsManager;

    public AppSettingsReadService(ISettingsManager<AppSettings> settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public string GetFontFamily() => _settingsManager.Settings.FontFamily;
    public string GetNewSongWindowPosition() => _settingsManager.Settings.NewSongWindowPosition;
    public string GetAudioOutputDeviceName() => _settingsManager.Settings.AudioOutputDeviceName;
    public IReadOnlyList<string> GetMusicFolders() => _settingsManager.Settings.MusicFolders.Select(x => x.FullPath).ToList();
    public bool GetScanOnStartup() => _settingsManager.Settings.ScanOnStartup;
    public bool GetEnableGlobalMediaKeys() => _settingsManager.Settings.EnableGlobalMediaKeys;
    public bool GetEnableCornerNowPlayingPopup() => _settingsManager.Settings.EnableCornerNowPlayingPopup;
    public short GetCornerTriggerSizePx() => _settingsManager.Settings.CornerTriggerSizePx;
    public short GetCornerTriggerDebounceMs() => _settingsManager.Settings.CornerTriggerDebounceMs;
    public float GetStartupVolume() => _settingsManager.Settings.StartupVolume;
    public bool GetStartMuted() => _settingsManager.Settings.StartMuted;
    public bool GetAutoCheckUpdatesOnStartup() => _settingsManager.Settings.AutoCheckUpdatesOnStartup;
    public bool GetAutoScanOnFolderAdd() => _settingsManager.Settings.AutoScanOnFolderAdd;
    public bool GetFolderBrowserStartAtLastLocation() => _settingsManager.Settings.FolderBrowserStartAtLastLocation;
    public string GetLastBrowsedFolder() => _settingsManager.Settings.LastBrowsedFolder;
    public IReadOnlyList<string> GetPinnedFolders() => _settingsManager.Settings.PinnedFolders.ToList();
}
