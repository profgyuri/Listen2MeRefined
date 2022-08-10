using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Core.Interfaces;

public interface IPlaylistReference
{
    void PassPlaylist(ref ObservableCollection<AudioModel> playlist);
}