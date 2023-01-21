using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class AdvancedSearchNotification : INotification
{
    public List<string> Filters { get; }
    public bool MatchAll { get; }

    public AdvancedSearchNotification(List<string> filters, bool matchAll = true)
    {
        Filters = filters;
        MatchAll = matchAll;
    }
}