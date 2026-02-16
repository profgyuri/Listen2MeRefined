namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class AdvancedSearchNotification : INotification
{
    public List<AdvancedFilter> Filters { get; }
    public bool MatchAll { get; }

    public AdvancedSearchNotification(List<AdvancedFilter> filters, bool matchAll = true)
    {
        Filters = filters;
        MatchAll = matchAll;
    }
}