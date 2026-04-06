using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Playback;

/// <summary>
///     Contract for controlling any media element.
/// </summary>
public interface IMusicPlayerController
{
    double CurrentTime { get; set; }

    float Volume { get; set; }

    RepeatMode RepeatMode { get; set; }

    /// <summary>
    ///     Gets a value indicating whether a song is currently loaded in the player.
    /// </summary>
    bool HasCurrentSong { get; }

    /// <summary>
    ///     Play or pause the playback.
    /// </summary>
    Task PlayPauseAsync();

    /// <summary>
    ///     Stop the playback and jump to start.
    /// </summary>
    void Stop();

    /// <summary>
    ///     Jump to the next media element.
    /// </summary>
    Task NextAsync();

    /// <summary>
    ///     Jump to the previous media element.
    /// </summary>
    Task PreviousAsync();

    /// <summary>
    ///     Jump to the specified media element.
    /// </summary>
    /// <param name="index">The position of the media element.</param>
    Task JumpToIndexAsync(int index);

    /// <summary>
    ///     Randomizes the order of the elements.
    /// </summary>
    Task Shuffle();
}