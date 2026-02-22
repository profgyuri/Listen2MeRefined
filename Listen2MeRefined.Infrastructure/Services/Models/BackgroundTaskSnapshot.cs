namespace Listen2MeRefined.Infrastructure.Services.Models;

public sealed record BackgroundTaskSnapshot(
    bool IsVisible,
    BackgroundTaskItem? PrimaryTask,
    int QueuedCount);
