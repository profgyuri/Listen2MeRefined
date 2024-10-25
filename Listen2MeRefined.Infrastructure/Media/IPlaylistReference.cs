namespace Listen2MeRefined.Infrastructure.Media;
using System.Collections.ObjectModel;

public interface IPlaylistReference
{
    void PassPlaylist(ref ObservableCollection<AudioModel> playlist);
}