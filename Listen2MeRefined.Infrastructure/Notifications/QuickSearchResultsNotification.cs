namespace Listen2MeRefined.Infrastructure.Notifications;

using Listen2MeRefined.Infrastructure.Data.Models;
using MediatR;

public class QuickSearchResultsNotification : INotification
{
    public QuickSearchResultsNotification(IEnumerable<AudioModel> results)
    {
        Results = results;
    }

    public IEnumerable<AudioModel> Results { get; }
}
