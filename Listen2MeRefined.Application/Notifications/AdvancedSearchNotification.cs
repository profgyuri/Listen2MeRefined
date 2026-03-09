using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using MediatR;

namespace Listen2MeRefined.Application.Notifications;

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
