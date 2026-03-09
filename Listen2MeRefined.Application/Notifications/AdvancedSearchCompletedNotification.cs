using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class AdvancedSearchCompletedNotification : INotification
{
    public int ResultCount { get; }

    public AdvancedSearchCompletedNotification(int resultCount)
    {
        ResultCount = resultCount;
    }
}
