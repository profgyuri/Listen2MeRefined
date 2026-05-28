using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist.Queuing;

public class PlaylistQueuing : IPlaylistQueuing
{
    public required IPlaylistQueue ActiveQueue { get; set; }
    public AudioModel? SelectedSong { get; set; }
}