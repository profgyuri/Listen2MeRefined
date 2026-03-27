namespace Listen2MeRefined.Application.Playback;

/// <summary>
/// Represents the outcome of a requested playback volume transition.
/// </summary>
/// <param name="Volume">The resulting volume in the 0..1 range.</param>
/// <param name="IsMuted">A value that indicates whether playback is muted after the change.</param>
/// <param name="HasVolumeChanged">A value that indicates whether the request changed effective volume state.</param>
public sealed record PlaybackVolumeChange(float Volume, bool IsMuted, bool HasVolumeChanged);
