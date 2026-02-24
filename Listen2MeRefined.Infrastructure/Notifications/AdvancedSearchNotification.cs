using Listen2MeRefined.Infrastructure.Searching;

namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class AdvancedSearchNotification : INotification
{
    public List<AdvancedFilter> Filters { get; }
    public SearchMatchMode MatchMode { get; }

    public AdvancedSearchNotification(List<AdvancedFilter> filters, SearchMatchMode matchMode = SearchMatchMode.All)
    {
        Filters = filters;
        MatchMode = matchMode;
    }
}
