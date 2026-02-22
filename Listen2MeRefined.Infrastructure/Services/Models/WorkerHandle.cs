namespace Listen2MeRefined.Infrastructure.Services.Models;

public readonly record struct WorkerHandle(Guid TaskId, Guid WorkerId);
