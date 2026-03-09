using Listen2MeRefined.Core.Models;
using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public class QuickSearchResultsNotification : INotification
{
    public QuickSearchResultsNotification(IEnumerable<AudioModel> results)
    {
        Results = results;
    }

    public IEnumerable<AudioModel> Results { get; }
}
