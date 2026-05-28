using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist.Queuing;

/// <summary>
///     Provides services to handle individual playlists.
/// </summary>
public interface IQueuing
{
    /// <summary>
    ///     Gets or sets the active playlist queue.
    /// </summary>
    IPlaylistQueue ActiveQueue { get; set; }
    
    /// <summary>
    ///     Gets or sets the currently selected song.
    /// </summary>
    AudioModel? SelectedSong { get; set; }
}