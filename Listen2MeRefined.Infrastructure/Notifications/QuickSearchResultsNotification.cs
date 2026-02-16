namespace Listen2MeRefined.Infrastructure.Notifications;

public class QuickSearchResultsNotification : INotification
{
    public QuickSearchResultsNotification(IEnumerable<AudioModel> results)
    {
        Results = results;
    }

    public IEnumerable<AudioModel> Results { get; }
}
