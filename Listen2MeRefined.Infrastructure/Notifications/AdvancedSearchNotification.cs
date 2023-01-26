using Listen2MeRefined.Infrastructure.Data;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

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