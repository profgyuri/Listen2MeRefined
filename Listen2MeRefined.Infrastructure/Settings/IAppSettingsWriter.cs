using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Scanning.Folders;

namespace Listen2MeRefined.Infrastructure.Settings;

/// <summary>
/// Provides typed write operations for persisted application settings.
/// </summary>
public interface IAppSettingsWriter
{
    /// <summary>Sets whether startup folder scan is enabled.</summary>
    void SetScanOnStartup(bool value);
    /// <summary>Sets the application font family.</summary>
    void SetFontFamily(string value);
    /// <summary>Sets the new-song window position mode.</summary>
    void SetNewSongWindowPosition(string value);
    /// <summary>Sets the audio output device name.</summary>
    void SetAudioOutputDeviceName(string value);
    /// <summary>Sets whether global media keys are enabled.</summary>
    void SetEnableGlobalMediaKeys(bool value);
    /// <summary>Sets whether corner now-playing popup is enabled.</summary>
    void SetEnableCornerNowPlayingPopup(bool value);
    /// <summary>Sets the corner trigger size in pixels.</summary>
    void SetCornerTriggerSizePx(short value);
    /// <summary>Sets the corner trigger debounce in milliseconds.</summary>
    void SetCornerTriggerDebounceMs(short value);
    /// <summary>Sets the startup playback volume value between 0 and 1.</summary>
    void SetStartupVolume(float value);
    /// <summary>Sets whether playback should start muted.</summary>
    void SetStartMuted(bool value);
    /// <summary>Sets whether automatic update checks on startup are enabled.</summary>
    void SetAutoCheckUpdatesOnStartup(bool value);
    /// <summary>Sets whether a folder should be auto-scanned when added.</summary>
    void SetAutoScanOnFolderAdd(bool value);
    /// <summary>Sets whether background task percentage should be shown in the title bar.</summary>
    void SetShowTaskPercentage(bool value);
    /// <summary>Sets title-bar percentage reporting interval in percent points.</summary>
    void SetTaskPercentageReportInterval(short value);
    /// <summary>Sets whether scan milestone count text is shown in the title bar.</summary>
    void SetShowScanMilestoneCount(bool value);
    /// <summary>Sets scan milestone count interval in files.</summary>
    void SetScanMilestoneInterval(short value);
    /// <summary>Sets whether milestone counts are based on processed or remaining file counts.</summary>
    void SetScanMilestoneBasis(TaskStatusCountBasis value);
    /// <summary>Sets whether folder browser should start at last location.</summary>
    void SetFolderBrowserStartAtLastLocation(bool value);
    /// <summary>Sets the last browsed folder path.</summary>
    void SetLastBrowsedFolder(string path);
    /// <summary>Sets configured music folder paths.</summary>
    void SetMusicFolders(IEnumerable<string> folders);
    /// <summary>Sets configured music folder scan requests.</summary>
    void SetMusicFolders(IEnumerable<FolderScanRequest> folders);
    /// <summary>Sets recursion flag for a specific configured folder.</summary>
    void SetFolderIncludeSubdirectories(string path, bool includeSubdirectories);
    /// <summary>Sets configured pinned folder paths.</summary>
    void SetPinnedFolders(IEnumerable<string> folders);
    /// <summary>Sets search-results transfer mode for default playlist tab.</summary>
    void SetSearchResultsTransferMode(SearchResultsTransferMode mode);
    /// <summary>Sets current theme mode.</summary>
    void SetThemeMode(string value);
    /// <summary>Sets current accent color name.</summary>
    void SetAccentColor(string value);
}
