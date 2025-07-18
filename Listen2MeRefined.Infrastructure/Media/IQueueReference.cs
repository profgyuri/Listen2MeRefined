namespace Listen2MeRefined.Infrastructure.Media;
using System.Collections.ObjectModel;

public interface IQueueReference
{
    void PassQueue(ref ObservableCollection<AudioModel> queue);
}