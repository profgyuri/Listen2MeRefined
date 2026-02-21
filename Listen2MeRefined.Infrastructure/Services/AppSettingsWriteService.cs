using Listen2MeRefined.Infrastructure.Storage;

namespace Listen2MeRefined.Infrastructure.Services;

using Contracts;

public sealed class AppSettingsWriteService : IAppSettingsWriteService
{
    private readonly ISettingsManager<AppSettings> _settingsManager;

    public AppSettingsWriteService(ISettingsManager<AppSettings> settingsManager)
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

    public void SetAutoScanOnFolderAdd(bool value)
    {
        _settingsManager.SaveSettings(s => s.AutoScanOnFolderAdd = value);
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
            .Select(x => new MusicFolderModel(x))
            .ToList();
        _settingsManager.SaveSettings(s => s.MusicFolders = normalized);
    }

    public void SetPinnedFolders(IEnumerable<string> folders)
    {
        var normalized = Normalize(folders);
        _settingsManager.SaveSettings(s => s.PinnedFolders = normalized);
    }

    private static List<string> Normalize(IEnumerable<string> folders)
    {
        return folders
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
