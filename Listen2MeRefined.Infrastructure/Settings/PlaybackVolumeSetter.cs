using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;

namespace Listen2MeRefined.Infrastructure.Settings;

/// <summary>
/// Applies playback volume and mute transitions and persists startup defaults.
/// </summary>
public sealed class PlaybackVolumeSetter : IPlaybackVolumeSetter
{
    private const float VolumeEpsilon = 0.0001f;
    private const float DefaultRestoreVolume = 0.7f;

    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IPlaybackDefaultsService _playbackDefaultsService;
    private float _lastNonZeroVolume = DefaultRestoreVolume;
    private bool _isMuted;

    public PlaybackVolumeSetter(
        IMusicPlayerController musicPlayerController,
        IPlaybackDefaultsService playbackDefaultsService)
    {
        _musicPlayerController = musicPlayerController;
        _playbackDefaultsService = playbackDefaultsService;
    }

    /// <summary>
    /// Applies startup volume defaults and initializes the playback volume state.
    /// </summary>
    /// <returns>The initialized playback volume state.</returns>
    public PlaybackVolumeState ApplyStartupDefaults()
    {
        var (startupVolume, startsMuted) = _playbackDefaultsService.LoadStartupDefaults();
        if (startupVolume > VolumeEpsilon)
        {
            _lastNonZeroVolume = startupVolume;
        }

        _musicPlayerController.Volume = startsMuted ? 0f : startupVolume;
        _isMuted = startsMuted || startupVolume <= VolumeEpsilon;

        return new PlaybackVolumeState(_musicPlayerController.Volume, _isMuted);
    }

    /// <summary>
    /// Applies a volume change request and updates mute state when thresholds are crossed.
    /// </summary>
    /// <param name="requestedVolume">The requested volume in the 0..1 range.</param>
    /// <returns>The resulting volume change details.</returns>
    public PlaybackVolumeChange SetVolume(float requestedVolume)
    {
        var clampedValue = Math.Clamp(requestedVolume, 0f, 1f);
        var previousVolume = _musicPlayerController.Volume;
        if (Math.Abs(previousVolume - clampedValue) < VolumeEpsilon)
        {
            return new PlaybackVolumeChange(previousVolume, _isMuted, HasVolumeChanged: false);
        }

        _musicPlayerController.Volume = clampedValue;
        if (clampedValue > VolumeEpsilon)
        {
            _lastNonZeroVolume = clampedValue;
        }

        if (_isMuted && clampedValue > VolumeEpsilon)
        {
            _isMuted = false;
        }
        else if (!_isMuted && clampedValue <= VolumeEpsilon)
        {
            _isMuted = true;
        }

        _playbackDefaultsService.PersistPlaybackDefaults(clampedValue, _isMuted);
        return new PlaybackVolumeChange(clampedValue, _isMuted, HasVolumeChanged: true);
    }

    /// <summary>
    /// Toggles the mute state and applies the corresponding volume update.
    /// </summary>
    /// <returns>The resulting volume change details.</returns>
    public PlaybackVolumeChange ToggleMute()
    {
        if (_isMuted)
        {
            var restoredVolume = _lastNonZeroVolume > VolumeEpsilon ? _lastNonZeroVolume : DefaultRestoreVolume;
            _musicPlayerController.Volume = restoredVolume;
            _isMuted = false;
            _playbackDefaultsService.PersistPlaybackDefaults(restoredVolume, isMuted: false);
            return new PlaybackVolumeChange(restoredVolume, _isMuted, HasVolumeChanged: true);
        }

        var currentVolume = _musicPlayerController.Volume;
        if (currentVolume > VolumeEpsilon)
        {
            _lastNonZeroVolume = currentVolume;
        }

        _musicPlayerController.Volume = 0f;
        _isMuted = true;
        _playbackDefaultsService.PersistPlaybackDefaults(0f, isMuted: true);
        return new PlaybackVolumeChange(0f, _isMuted, HasVolumeChanged: true);
    }

    /// <summary>
    /// Gets the current icon kind for the active volume and mute state.
    /// </summary>
    /// <returns>The Material icon kind name for the current state.</returns>
    public string GetVolumeIconKind()
    {
        var volume = _musicPlayerController.Volume;
        if (_isMuted || volume <= VolumeEpsilon)
        {
            return "VolumeOff";
        }

        if (volume < 0.34f)
        {
            return "VolumeLow";
        }

        return volume < 0.67f ? "VolumeMedium" : "VolumeHigh";
    }
}
