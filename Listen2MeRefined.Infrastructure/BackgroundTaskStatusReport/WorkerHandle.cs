namespace Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;

public readonly record struct WorkerHandle(Guid TaskId, Guid WorkerId);
