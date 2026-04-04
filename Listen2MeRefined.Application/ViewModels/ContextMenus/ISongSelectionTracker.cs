using System.Collections;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.ViewModels.ContextMenus;

/// <summary>
/// Tracks multi-selection state for a song list and publishes selection-change notifications.
/// </summary>
public interface ISongSelectionTracker
{
    IReadOnlyCollection<AudioModel> SelectedSongs { get; }
    void AddSelection(IList items);
    void RemoveSelection(IList items);
    void Clear();
}
