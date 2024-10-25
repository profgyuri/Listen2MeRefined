namespace Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

public sealed class AdvancedSearchNotification : INotification
{
    public List<ParameterizedQuery> Filters { get; }
    public bool MatchAll { get; }

    public AdvancedSearchNotification(List<ParameterizedQuery> filters, bool matchAll = true)
    {
        Filters = filters;
        MatchAll = matchAll;
    }
}