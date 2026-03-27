using System.Collections.ObjectModel;
using System.ComponentModel;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Exposes the shared playback queue state used by playlist-related view models.
/// </summary>
public interface IPlaylistQueueState : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the active playback queue.
    /// </summary>
    ObservableCollection<AudioModel> PlayList { get; }

    /// <summary>
    /// Gets the persistent default playlist.
    /// </summary>
    ObservableCollection<AudioModel> DefaultPlaylist { get; }

    /// <summary>
    /// Gets or sets the currently selected song.
    /// </summary>
    AudioModel? SelectedSong { get; set; }

    /// <summary>
    /// Gets or sets the selected index in the active playback queue.
    /// </summary>
    int SelectedIndex { get; set; }

    /// <summary>
    /// Gets the active named playlist identifier, or <see langword="null" /> when the default playlist is active.
    /// </summary>
    int? ActiveNamedPlaylistId { get; }

    /// <summary>
    /// Gets a value that indicates whether the default playlist is currently active.
    /// </summary>
    bool IsDefaultPlaylistActive { get; }
    
    /// <summary>
    /// Gets or sets the current song index in the active playback queue.
    /// </summary>
    int CurrentSongIndex { get; set; }
    
    /// <summary>
    /// Sets the active queue source identifier and notifies dependent properties when it changes.
    /// </summary>
    /// <param name="playlistId">The active named playlist identifier, or <see langword="null" /> for default queue.</param>
    void SetActiveNamedPlaylistId(int? playlistId);
}
