namespace Listen2MeRefined.Application.Threading;

public sealed record BackgroundTaskSnapshot(
    bool IsVisible,
    BackgroundTaskItem? PrimaryTask,
    int QueuedCount);
