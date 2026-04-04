using System.Collections.ObjectModel;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Utils;

/// <inheritdoc />
public sealed class ObservableCollectionUpdater : IObservableCollectionUpdater
{
    public void ReplaceIfPresent(ObservableCollection<AudioModel> collection, AudioModel updated)
    {
        var index = collection.IndexOf(updated);

        if (index >= 0)
        {
            collection.RemoveAt(index);
            collection.Insert(index, updated);
        }
    }
}
