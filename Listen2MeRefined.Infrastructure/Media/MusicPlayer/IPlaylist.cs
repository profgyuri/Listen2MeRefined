using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public interface IPlaylist
{
    IList<AudioModel> Items { get; }

    int Count { get; }

    int CurrentIndex { get; set; }

    AudioModel this[int index] { get; set; }

    bool Any();

    int IndexOf(AudioModel? audio);

    bool Remove(AudioModel audio);

    /// <summary>
    ///     Moves the item at the specified index to a new location in the collection.
    /// </summary>
    void Move(int oldIndex, int newIndex);

    void Shuffle();

}