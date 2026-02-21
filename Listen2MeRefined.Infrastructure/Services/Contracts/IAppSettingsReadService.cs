namespace Listen2MeRefined.Infrastructure.Services.Contracts;

/// <summary>
/// Provides typed read access to persisted application settings.
/// </summary>
public interface IAppSettingsReadService
{
    /// <summary>Gets the configured application font family.</summary>
    string GetFontFamily();
    /// <summary>Gets the configured new-song window position mode.</summary>
    string GetNewSongWindowPosition();
    /// <summary>Gets the configured audio output device name.</summary>
    string GetAudioOutputDeviceName();
    /// <summary>Gets configured music folder paths.</summary>
    IReadOnlyList<string> GetMusicFolders();
    /// <summary>Gets whether startup folder scan is enabled.</summary>
    bool GetScanOnStartup();
    /// <summary>Gets whether global media keys are enabled.</summary>
    bool GetEnableGlobalMediaKeys();
    /// <summary>Gets whether corner now-playing popup is enabled.</summary>
    bool GetEnableCornerNowPlayingPopup();
    /// <summary>Gets the corner trigger size in pixels.</summary>
    short GetCornerTriggerSizePx();
    /// <summary>Gets the corner trigger debounce in milliseconds.</summary>
    short GetCornerTriggerDebounceMs();
    /// <summary>Gets the startup playback volume value between 0 and 1.</summary>
    float GetStartupVolume();
    /// <summary>Gets whether playback should start muted.</summary>
    bool GetStartMuted();
    /// <summary>Gets whether automatic update checks on startup are enabled.</summary>
    bool GetAutoCheckUpdatesOnStartup();
    /// <summary>Gets whether a folder should be auto-scanned when added.</summary>
    bool GetAutoScanOnFolderAdd();
    /// <summary>Gets whether folder browser should start at last location.</summary>
    bool GetFolderBrowserStartAtLastLocation();
    /// <summary>Gets the last browsed folder path.</summary>
    string GetLastBrowsedFolder();
    /// <summary>Gets configured pinned folder paths.</summary>
    IReadOnlyList<string> GetPinnedFolders();
}
