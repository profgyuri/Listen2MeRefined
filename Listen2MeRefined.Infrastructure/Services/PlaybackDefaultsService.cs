using Listen2MeRefined.Infrastructure.Services.Models;

namespace Listen2MeRefined.Infrastructure.Services;

using Contracts;

public sealed class PlaybackDefaultsService : IPlaybackDefaultsService
{
    private readonly ISettingsManager<AppSettings> _settingsManager;

    public PlaybackDefaultsService(ISettingsManager<AppSettings> settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public PlaybackDefaultsSnapshot LoadStartupDefaults()
    {
        var settings = _settingsManager.Settings;
        var startupVolume = Math.Clamp(settings.StartupVolume, 0f, 1f);
        return new PlaybackDefaultsSnapshot(startupVolume, settings.StartMuted);
    }

    public void PersistPlaybackDefaults(float currentVolume, bool isMuted)
    {
        var clamped = Math.Clamp(currentVolume, 0f, 1f);
        _settingsManager.SaveSettings(settings =>
        {
            settings.StartMuted = isMuted;
            if (clamped > 0f)
            {
                settings.StartupVolume = clamped;
            }
        });
    }

    public int ToVolumePercent(float volume)
    {
        var clamped = Math.Clamp(volume, 0f, 1f);
        return (int)Math.Round(clamped * 100f);
    }

    public float FromVolumePercent(int volumePercent)
    {
        var clamped = Math.Clamp(volumePercent, 0, 100);
        return clamped / 100f;
    }
}
