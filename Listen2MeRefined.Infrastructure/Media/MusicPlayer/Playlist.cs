using System.Collections.ObjectModel;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class Playlist : IPlaylist
{
    private ObservableCollection<AudioModel> _items = new();
    public IList<AudioModel> Items => _items;
    public int Count => Items.Count;
    public int CurrentIndex { get; set; }

    public AudioModel this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }

    public bool Any()
    {
        return Items.Any();
    }

    public int IndexOf(AudioModel? audio)
    {
        return audio is null ? -1 : Items.IndexOf(audio);
    }

    public bool Remove(AudioModel audio)
    {
        return Items.Remove(audio);
    }
    
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex)
        {
            return;
        }
        
        var item = Items[oldIndex];
        Items.RemoveAt(oldIndex);
        Items.Insert(newIndex, item);
    }

    public void Shuffle()
    {
        Items.Shuffle();
    }

}