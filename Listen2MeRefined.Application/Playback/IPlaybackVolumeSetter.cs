namespace Listen2MeRefined.Application.Playback;

/// <summary>
/// Applies playback volume and mute transitions while keeping persisted defaults in sync.
/// </summary>
public interface IPlaybackVolumeSetter
{
    /// <summary>
    /// Applies startup volume defaults and initializes the playback volume state.
    /// </summary>
    /// <returns>The initialized playback volume state.</returns>
    PlaybackVolumeState ApplyStartupDefaults();

    /// <summary>
    /// Applies a volume change request and updates mute state when thresholds are crossed.
    /// </summary>
    /// <param name="requestedVolume">The requested volume in the 0..1 range.</param>
    /// <returns>The resulting volume change details.</returns>
    PlaybackVolumeChange SetVolume(float requestedVolume);

    /// <summary>
    /// Toggles the mute state and applies the corresponding volume update.
    /// </summary>
    /// <returns>The resulting volume change details.</returns>
    PlaybackVolumeChange ToggleMute();

    /// <summary>
    /// Gets the current icon kind for the active volume and mute state.
    /// </summary>
    /// <returns>The Material icon kind name for the current state.</returns>
    string GetVolumeIconKind();
}
