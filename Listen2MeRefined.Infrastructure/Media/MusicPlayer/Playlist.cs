using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class Playlist : IPlaylist
{
    private readonly ObservableCollection<AudioModel> _items = [];

    public Playlist()
    {
        _items.CollectionChanged += OnItemsCollectionChanged;
    }

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

        _items.Move(oldIndex, newIndex);
    }

    public void Shuffle()
    {
        Items.Shuffle();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Count == 0)
        {
            CurrentIndex = 0;
            return;
        }

        if (e.Action == NotifyCollectionChangedAction.Move && e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0)
        {
            if (CurrentIndex == e.OldStartingIndex)
            {
                CurrentIndex = e.NewStartingIndex;
            }
            else if (e.OldStartingIndex < CurrentIndex && CurrentIndex <= e.NewStartingIndex)
            {
                CurrentIndex--;
            }
            else if (e.NewStartingIndex <= CurrentIndex && CurrentIndex < e.OldStartingIndex)
            {
                CurrentIndex++;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldStartingIndex >= 0)
        {
            if (e.OldStartingIndex < CurrentIndex)
            {
                CurrentIndex--;
            }
            else if (e.OldStartingIndex == CurrentIndex && CurrentIndex >= Count)
            {
                CurrentIndex = Count - 1;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            CurrentIndex = 0;
        }

        if (CurrentIndex < 0)
        {
            CurrentIndex = 0;
        }
        else if (CurrentIndex >= Count)
        {
            CurrentIndex = Count - 1;
        }
    }
}
