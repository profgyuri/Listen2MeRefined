using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Settings;

public sealed class AppSettingsWriter : IAppSettingsWriter
{
    private readonly ISettingsManager<AppSettings> _settingsManager;

    public AppSettingsWriter(ISettingsManager<AppSettings> settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public void SetScanOnStartup(bool value)
    {
        _settingsManager.SaveSettings(s => s.ScanOnStartup = value);
    }

    public void SetFontFamily(string value)
    {
        _settingsManager.SaveSettings(s => s.FontFamily = value);
    }

    public void SetNewSongWindowPosition(string value)
    {
        _settingsManager.SaveSettings(s => s.NewSongWindowPosition = value);
    }

    public void SetAudioOutputDeviceName(string value)
    {
        _settingsManager.SaveSettings(s => s.AudioOutputDeviceName = value);
    }

    public void SetEnableGlobalMediaKeys(bool value)
    {
        _settingsManager.SaveSettings(s => s.EnableGlobalMediaKeys = value);
    }

    public void SetEnableCornerNowPlayingPopup(bool value)
    {
        _settingsManager.SaveSettings(s => s.EnableCornerNowPlayingPopup = value);
    }

    public void SetCornerTriggerSizePx(short value)
    {
        _settingsManager.SaveSettings(s => s.CornerTriggerSizePx = value);
    }

    public void SetCornerTriggerDebounceMs(short value)
    {
        _settingsManager.SaveSettings(s => s.CornerTriggerDebounceMs = value);
    }

    public void SetStartupVolume(float value)
    {
        _settingsManager.SaveSettings(s => s.StartupVolume = value);
    }

    public void SetStartMuted(bool value)
    {
        _settingsManager.SaveSettings(s => s.StartMuted = value);
    }

    public void SetAutoCheckUpdatesOnStartup(bool value)
    {
        _settingsManager.SaveSettings(s => s.AutoCheckUpdatesOnStartup = value);
    }

    public void SetUseCompactPlaylistView(bool value)
    {
        _settingsManager.SaveSettings(s => s.UseCompactPlaylistView = value);
    }

    public void SetAutoScanOnFolderAdd(bool value)
    {
        _settingsManager.SaveSettings(s => s.AutoScanOnFolderAdd = value);
    }

    public void SetShowTaskPercentage(bool value)
    {
        _settingsManager.SaveSettings(s => s.ShowTaskPercentage = value);
    }

    public void SetTaskPercentageReportInterval(short value)
    {
        _settingsManager.SaveSettings(s => s.TaskPercentageReportInterval = value);
    }

    public void SetShowScanMilestoneCount(bool value)
    {
        _settingsManager.SaveSettings(s => s.ShowScanMilestoneCount = value);
    }

    public void SetScanMilestoneInterval(short value)
    {
        _settingsManager.SaveSettings(s => s.ScanMilestoneInterval = value);
    }

    public void SetScanMilestoneBasis(TaskStatusCountBasis value)
    {
        _settingsManager.SaveSettings(s => s.ScanMilestoneBasis = value);
    }

    public void SetFolderBrowserStartAtLastLocation(bool value)
    {
        _settingsManager.SaveSettings(s => s.FolderBrowserStartAtLastLocation = value);
    }

    public void SetLastBrowsedFolder(string path)
    {
        _settingsManager.SaveSettings(s => s.LastBrowsedFolder = path);
    }

    public void SetMusicFolders(IEnumerable<string> folders)
    {
        var normalized = Normalize(folders)
            .Select(x => new FolderScanRequest(x, false));
        SetMusicFolders(normalized);
    }

    public void SetMusicFolders(IEnumerable<FolderScanRequest> folders)
    {
        var normalized = NormalizeFolderRequests(folders)
            .Select(x => new MusicFolderModel(x.Path, x.IncludeSubdirectories))
            .ToList();
        _settingsManager.SaveSettings(s => s.MusicFolders = normalized);
    }

    public void SetFolderIncludeSubdirectories(string path, bool includeSubdirectories)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalizedPath = path.Trim();
        _settingsManager.SaveSettings(s =>
        {
            var folder = s.MusicFolders
                .FirstOrDefault(x => x.FullPath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));
            if (folder is not null)
            {
                folder.IncludeSubdirectories = includeSubdirectories;
            }
        });
    }

    public void SetPinnedFolders(IEnumerable<string> folders)
    {
        var normalized = Normalize(folders);
        _settingsManager.SaveSettings(s => s.PinnedFolders = normalized);
    }

    public void SetSearchResultsTransferMode(SearchResultsTransferMode mode)
    {
        _settingsManager.SaveSettings(s => s.SearchResultsTransferMode = mode);
    }

    public void SetMutedDroppedSongFolders(IEnumerable<string> folders)
    {
        var normalized = Normalize(folders);
        _settingsManager.SaveSettings(s => s.MutedDroppedSongFolders = normalized);
    }

    public void SetThemeMode(string value)
    {
        _settingsManager.SaveSettings(s => s.ThemeMode = value);
    }

    public void SetAccentColor(string value)
    {
        _settingsManager.SaveSettings(s => s.AccentColor = value);
    }

    private static List<string> Normalize(IEnumerable<string> folders)
    {
        return folders
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<FolderScanRequest> NormalizeFolderRequests(IEnumerable<FolderScanRequest> folders)
    {
        var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            if (string.IsNullOrWhiteSpace(folder.Path))
            {
                continue;
            }

            var path = folder.Path.Trim();
            if (map.TryGetValue(path, out var include))
            {
                map[path] = include || folder.IncludeSubdirectories;
            }
            else
            {
                map[path] = folder.IncludeSubdirectories;
            }
        }

        return map
            .Select(x => new FolderScanRequest(x.Key, x.Value))
            .ToList();
    }
}
