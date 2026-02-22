namespace Listen2MeRefined.Infrastructure.Services.Models;

public sealed record BackgroundTaskItem(
    string TaskKey,
    string DisplayName,
    BackgroundTaskState State,
    bool IsDeterminate,
    int ProcessedUnits,
    int? TotalUnits,
    int? Percent,
    string? CountText,
    string? Message,
    DateTimeOffset StartedAtUtc);
