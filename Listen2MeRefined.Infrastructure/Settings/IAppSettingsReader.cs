using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Scanning.Folders;

namespace Listen2MeRefined.Infrastructure.Settings;

/// <summary>
/// Provides typed read access to persisted application settings.
/// </summary>
public interface IAppSettingsReader
{
    /// <summary>Gets the configured application font family.</summary>
    string GetFontFamily();
    /// <summary>Gets the configured new-song window position mode.</summary>
    string GetNewSongWindowPosition();
    /// <summary>Gets the configured audio output device name.</summary>
    string GetAudioOutputDeviceName();
    /// <summary>Gets configured music folder paths.</summary>
    IReadOnlyList<string> GetMusicFolders();
    /// <summary>Gets configured music folder scan requests.</summary>
    IReadOnlyList<FolderScanRequest> GetMusicFolderRequests();
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
    /// <summary>Gets whether playlist uses compact visual rows.</summary>
    bool GetUseCompactPlaylistView();
    /// <summary>Gets whether a folder should be auto-scanned when added.</summary>
    bool GetAutoScanOnFolderAdd();
    /// <summary>Gets whether background task percentage should be shown in the title bar.</summary>
    bool GetShowTaskPercentage();
    /// <summary>Gets title-bar percentage reporting interval in percent points.</summary>
    short GetTaskPercentageReportInterval();
    /// <summary>Gets whether scan milestone count text is shown in the title bar.</summary>
    bool GetShowScanMilestoneCount();
    /// <summary>Gets scan milestone count interval in files.</summary>
    short GetScanMilestoneInterval();
    /// <summary>Gets whether milestone counts are based on processed or remaining file counts.</summary>
    TaskStatusCountBasis GetScanMilestoneBasis();
    /// <summary>Gets whether folder browser should start at last location.</summary>
    bool GetFolderBrowserStartAtLastLocation();
    /// <summary>Gets the last browsed folder path.</summary>
    string GetLastBrowsedFolder();
    /// <summary>Gets configured pinned folder paths.</summary>
    IReadOnlyList<string> GetPinnedFolders();
    /// <summary>Gets search-results transfer mode for default playlist tab.</summary>
    SearchResultsTransferMode GetSearchResultsTransferMode();
    /// <summary>Gets current theme mode.</summary>
    string GetThemeMode();
    /// <summary>Gets current accent color name.</summary>
    string GetAccentColor();
}
