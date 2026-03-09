using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Threading;

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
