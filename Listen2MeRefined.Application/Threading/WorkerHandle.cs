namespace Listen2MeRefined.Application.Threading;

public readonly record struct WorkerHandle(Guid TaskId, Guid WorkerId);
