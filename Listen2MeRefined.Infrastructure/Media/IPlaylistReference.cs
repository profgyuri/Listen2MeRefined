using System.Collections.ObjectModel;

namespace Listen2MeRefined.Infrastructure.Media;

public interface IPlaylistReference
{
    void PassPlaylist(ref ObservableCollection<AudioModel> playlist);
}