using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

/// <summary>
///     Should be raised when the current song changes in the player.
/// </summary>
public sealed class CurrentSongNotification : INotification
{
    /// <summary>
    ///     The current song.
    /// </summary>
    public AudioModel Audio { get; }

    /// <param name="audio">The current song. </param>
    public CurrentSongNotification(AudioModel audio)
    {
        Audio = audio;
    }
}