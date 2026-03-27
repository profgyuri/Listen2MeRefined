namespace Listen2MeRefined.Application.Playback;

/// <summary>
/// Represents the current playback volume and mute state.
/// </summary>
/// <param name="Volume">The active volume in the 0..1 range.</param>
/// <param name="IsMuted">A value that indicates whether playback is muted.</param>
public sealed record PlaybackVolumeState(float Volume, bool IsMuted);
