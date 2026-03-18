namespace Listen2MeRefined.Application.ViewModels.Shells;

public sealed class SettingsShellNavigationProvider : ISettingsShellNavigationProvider
{
    public IReadOnlyList<SettingsShellNavigationItem> CreateNavigationItems() =>
    [
        new("settings/general", "General", "Tune"),
        new("settings/playback", "Playback", "VolumeHigh"),
        new("settings/library", "Library", "FolderMusicOutline"),
        new("settings/playlists", "Playlists", "PlaylistMusic"),
        new("settings/hooksAndAlerts", "Hooks & Alerts", "BellOutline"),
        new("settings/advanced", "Advanced", "AlertCircleOutline")
    ];
}
