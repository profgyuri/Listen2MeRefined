using System.Collections.ObjectModel;
using System.ComponentModel;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Stores shared playlist queue state and notifies subscribers when state changes.
/// </summary>
public sealed class PlaylistQueueState : IPlaylistQueueState
{
    private readonly IPlaylistQueue _playList;
    private readonly ObservableCollection<AudioModel> _defaultPlaylist = [];
    private int? _activeNamedPlaylistId;

    public PlaylistQueueState(IPlaylistQueue playList)
    {
        _playList = playList;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AudioModel> PlayList =>
        _playList.Items as ObservableCollection<AudioModel> ??
        throw new InvalidOperationException("PlayList is not an ObservableCollection");

    public ObservableCollection<AudioModel> DefaultPlaylist => _defaultPlaylist;

    public AudioModel? SelectedSong
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(nameof(SelectedSong));
        }
    }

    public int SelectedIndex
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged(nameof(SelectedIndex));
        }
    } = -1;

    public int? ActiveNamedPlaylistId => _activeNamedPlaylistId;

    public bool IsDefaultPlaylistActive => _activeNamedPlaylistId is null;

    public int CurrentSongIndex { get; set; } = -1;

    /// <summary>
    /// Sets the active queue source identifier and notifies dependent properties when it changes.
    /// </summary>
    /// <param name="playlistId">The active named playlist identifier, or <see langword="null" /> for default queue.</param>
    public void SetActiveNamedPlaylistId(int? playlistId)
    {
        if (_activeNamedPlaylistId == playlistId)
        {
            return;
        }

        _activeNamedPlaylistId = playlistId;
        OnPropertyChanged(nameof(ActiveNamedPlaylistId));
        OnPropertyChanged(nameof(IsDefaultPlaylistActive));
    }

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
