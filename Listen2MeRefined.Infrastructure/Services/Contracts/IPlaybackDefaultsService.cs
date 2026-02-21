using Listen2MeRefined.Infrastructure.Services.Models;

namespace Listen2MeRefined.Infrastructure.Services.Contracts;

/// <summary>
/// Provides read/write policy for playback startup defaults and conversions.
/// </summary>
public interface IPlaybackDefaultsService
{
    /// <summary>Loads startup playback defaults from persisted settings.</summary>
    PlaybackDefaultsSnapshot LoadStartupDefaults();
    /// <summary>Persists playback defaults based on current volume and mute state.</summary>
    void PersistPlaybackDefaults(float currentVolume, bool isMuted);
    /// <summary>Converts a 0..1 volume value to percentage.</summary>
    int ToVolumePercent(float volume);
    /// <summary>Converts percentage volume to a 0..1 value.</summary>
    float FromVolumePercent(int volumePercent);
}
