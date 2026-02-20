namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class AdvancedSearchCompletedNotification : INotification
{
    public int ResultCount { get; }

    public AdvancedSearchCompletedNotification(int resultCount)
    {
        ResultCount = resultCount;
    }
}
