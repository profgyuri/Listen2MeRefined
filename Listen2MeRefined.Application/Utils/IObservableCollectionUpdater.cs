using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Utils;

/// <summary>
/// Provides in-place replacement of items in an <see cref="ObservableCollection{T}"/>.
/// </summary>
public interface IObservableCollectionUpdater
{
    /// <summary>
    /// Replaces the item equal to <paramref name="updated"/> (by model equality) in <paramref name="collection"/>.
    /// </summary>
    void ReplaceIfPresent(ObservableCollection<AudioModel> collection, AudioModel updated);
}
