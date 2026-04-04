using System.Collections;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.ViewModels.ContextMenus;

/// <inheritdoc />
public sealed class SongSelectionTracker : ISongSelectionTracker
{
    private readonly HashSet<AudioModel> _selected = [];
    private readonly Action _onSelectionChanged;

    public SongSelectionTracker(Action onSelectionChanged)
    {
        ArgumentNullException.ThrowIfNull(onSelectionChanged);
        _onSelectionChanged = onSelectionChanged;
    }

    public IReadOnlyCollection<AudioModel> SelectedSongs => _selected.ToArray();

    public void AddSelection(IList items)
    {
        var songs = items.Cast<AudioModel>().ToArray();
        foreach (var song in songs)
        {
            _selected.Add(song);
        }

        _onSelectionChanged();
    }

    public void RemoveSelection(IList items)
    {
        var songs = items.Cast<AudioModel>().ToArray();
        foreach (var song in songs)
        {
            _selected.Remove(song);
        }

        _onSelectionChanged();
    }

    public void Clear()
    {
        _selected.Clear();
        _onSelectionChanged();
    }
}
