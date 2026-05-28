using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playback;

public interface IPlaybackQueueService
{
    /// <summary>
    ///     Returns the currently loaded track.
    /// </summary>
    AudioModel? GetCurrentTrack();

    /// <summary>
    ///     Returns the next track.
    /// </summary>
    AudioModel? GetNextTrack();

    /// <summary>
    ///     Returns the previous track.
    /// </summary>
    AudioModel? GetPreviousTrack();

    /// <summary>
    ///     Returns the found track at the specified index.
    /// </summary>
    /// <param name="index">The index of the track.</param>
    AudioModel? GetTrackAtIndex(int index);

    /// <summary>
    ///     Removes the specified track from the queue.
    /// </summary>
    /// <param name="track">The track to remove.</param>
    /// <returns>True if the track was removed, false otherwise.</returns>
    bool RemoveTrack(AudioModel track);

    /// <summary>
    ///     Returns whether the current track is the last in the queue.
    /// </summary>
    bool IsAtLastTrack();
}