using Listen2MeRefined.Core.Enums;
using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

/// <summary>
/// Represents the result of attempting to load a track.
/// </summary>
public sealed record TrackLoadResult(
    TrackLoadStatus Status,
    WaveStream? Reader = null,
    string? Reason = null)
{
    /// <summary>
    /// Gets a value indicating whether the load operation produced a playable reader.
    /// </summary>
    public bool IsSuccess => Status == TrackLoadStatus.Success && Reader is not null;

    /// <summary>
    /// Creates a successful track load result for the provided reader.
    /// </summary>
    /// <param name="reader">The initialized track reader.</param>
    /// <returns>A successful track load result.</returns>
    public static TrackLoadResult Success(WaveStream reader) => new(TrackLoadStatus.Success, reader);
}
