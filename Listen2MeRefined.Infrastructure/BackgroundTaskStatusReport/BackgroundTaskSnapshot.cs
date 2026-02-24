namespace Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;

public sealed record BackgroundTaskSnapshot(
    bool IsVisible,
    BackgroundTaskItem? PrimaryTask,
    int QueuedCount);
