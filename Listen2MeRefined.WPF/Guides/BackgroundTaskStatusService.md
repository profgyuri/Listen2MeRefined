# BackgroundTaskStatusService Guide

This document explains when and how to use `IBackgroundTaskStatusService` for long-running work, including multi-threaded task execution.

## Purpose

`IBackgroundTaskStatusService` provides a single status pipeline for background operations that need visible progress in the main title bar.

It supports:
- Multiple concurrent logical tasks
- One logical task split across multiple workers/threads
- Determinate and indeterminate progress
- Completion/failure terminal visibility
- Runtime presentation settings (percent/count behavior)

Primary contract:
- `Listen2MeRefined.Infrastructure/Services/Contracts/IBackgroundTaskStatusService.cs`

Implementation:
- `Listen2MeRefined.Infrastructure/Services/BackgroundTaskStatusService.cs`

## Key concepts

### Logical task

A logical task is a user-facing unit of work, started with:

- `StartTask(taskKey, displayName, progressKind, priority)`

Examples:
- "Scanning library"
- "Rebuilding waveform cache"
- "Importing metadata"

### Worker

A worker is a sub-unit of one logical task.  
Use workers when a task is parallelized (for example, one worker per folder, shard, queue partition, or thread).

- `RegisterWorker(taskHandle, workerKey, totalUnits?)`
- `ReportWorker(workerHandle, processedDelta, totalUnits?, message?)`
- `CompleteWorker(workerHandle)`
- `FailWorker(workerHandle, error)`

### Snapshot

Consumers (typically view models) use:

- `GetSnapshot()`
- `SnapshotChanged` event

Snapshot model:
- `BackgroundTaskSnapshot` has:
  - `IsVisible`
  - `PrimaryTask`
  - `QueuedCount`

Task model:
- `BackgroundTaskItem` has:
  - task identity/name
  - `Running/Completed/Failed`
  - processed/total/percent
  - optional milestone count text
  - optional message

## When to use this service

Use it for operations that are:
- Long enough to be noticeable
- Asynchronous/background by design
- Useful for user trust/feedback

Good candidates:
- File and folder scanning
- Bulk metadata or DB reconciliation
- Background indexing/caching
- Batch import/export

Avoid using it for:
- Very short tasks (few ms)
- High-frequency micro-operations where status noise would dominate UX

## Lifecycle pattern (required)

Use this sequence:

1. `StartTask(...)`
2. Register one or more workers
3. Report progress using `processedDelta`
4. Mark workers complete/fail
5. Explicitly call `CompleteTask(...)` or `FailTask(...)` at logical task end

Important:
- `CompleteWorker(...)` **does not auto-complete** the logical task.
- You must finalize the task explicitly with `CompleteTask(...)` or `FailTask(...)`.

## Determinate vs indeterminate progress

### Determinate (`TaskProgressKind.Determinate`)

Use when total work units are known or can be computed.

The service computes aggregate values:
- `ProcessedUnits = sum(worker.ProcessedUnits)`
- `TotalUnits = sum(worker.TotalUnits)` (if all workers have totals)
- `Percent = floor(Processed * 100 / Total)` (then presentation-throttled)

### Indeterminate (`TaskProgressKind.Indeterminate`)

Use when total cannot be reliably estimated.

In this mode:
- `TotalUnits` is not required
- `Percent` is omitted
- message updates can still be shown

## Multi-threaded tasks

For parallel execution, create one logical task and multiple workers.

Recommended worker mapping:
- One worker per independent parallel lane:
  - folder
  - partition
  - queue consumer
  - thread-owned batch

Why:
- accurate weighted aggregation
- clean progress math
- easy diagnostics via `workerKey`

### Aggregation behavior

The service aggregates across all workers under the same task:
- weighted by units, not average worker percentage
- robust for uneven worker sizes

Example:
- Worker A: total 100, processed 50
- Worker B: total 300, processed 150
- Aggregate: processed 200 / total 400 => 50%

## Runtime settings behavior

Presentation is controlled through `AppSettings` and read live by the service.

Relevant settings:
- `ShowTaskPercentage`
- `TaskPercentageReportInterval` (`1..25`)
- `ShowScanMilestoneCount`
- `ScanMilestoneInterval` (`5..500`)
- `ScanMilestoneBasis` (`Processed` / `Remaining`)

`RefreshSnapshot()` can be used by settings code paths to force immediate re-projection after updates.

## Percentage throttling

When enabled, displayed percent is stepped by interval:
- interval 1: 17% displays 17%
- interval 5: 17% displays 15%

Terminal completed determinate tasks display `100%`.

## Milestone count behavior

Milestone text is optional and based on boundary crossing, not exact divisibility checks.

Processed basis:
- compares `floor(previousProcessed / X)` vs `floor(currentProcessed / X)`
- when crossed, shows the latest crossed boundary (example: `"75 processed"`)

Remaining basis:
- compares buckets for remaining units
- when remaining drops across boundaries, shows latest crossed remaining boundary (example: `"100 remaining"`)

If one update jumps across multiple milestones, only the latest crossed milestone is displayed.

## Error and terminal visibility

Completion/failure task cards are shown for a short terminal visibility window (default 5 seconds), then hidden automatically.

Use:
- `FailWorker(...)` when a worker fails and the task should fail
- `FailTask(...)` when orchestration-level failure occurs

## Integration example: single-threaded batch

```csharp
public async Task RebuildCacheAsync(IEnumerable<string> files)
{
    var paths = files.ToArray();
    var task = _backgroundTaskStatus.StartTask(
        "cache-rebuild",
        "Rebuilding cache",
        TaskProgressKind.Determinate);

    var worker = _backgroundTaskStatus.RegisterWorker(task, "main", paths.Length);

    try
    {
        foreach (var path in paths)
        {
            await _cacheService.ProcessAsync(path);
            _backgroundTaskStatus.ReportWorker(worker, processedDelta: 1);
        }

        _backgroundTaskStatus.CompleteWorker(worker);
        _backgroundTaskStatus.CompleteTask(task, "Cache rebuild completed.");
    }
    catch (Exception ex)
    {
        _backgroundTaskStatus.FailWorker(worker, ex.Message);
        _backgroundTaskStatus.FailTask(task, $"Cache rebuild failed: {ex.Message}");
        throw;
    }
}
```

## Integration example: multi-threaded task

```csharp
public async Task ScanPartitionsAsync(IReadOnlyList<IReadOnlyList<string>> partitions)
{
    var task = _backgroundTaskStatus.StartTask(
        "partition-scan",
        "Scanning partitions",
        TaskProgressKind.Determinate);

    var workers = new List<(WorkerHandle Handle, IReadOnlyList<string> Files)>();
    for (var i = 0; i < partitions.Count; i++)
    {
        var files = partitions[i];
        var handle = _backgroundTaskStatus.RegisterWorker(task, $"partition-{i}", files.Count);
        workers.Add((handle, files));
    }

    try
    {
        await Parallel.ForEachAsync(workers, async (w, ct) =>
        {
            foreach (var file in w.Files)
            {
                await _analyzer.AnalyzeAsync(file);
                _backgroundTaskStatus.ReportWorker(w.Handle, 1);
            }

            _backgroundTaskStatus.CompleteWorker(w.Handle);
        });

        _backgroundTaskStatus.CompleteTask(task, "Partition scan completed.");
    }
    catch (Exception ex)
    {
        _backgroundTaskStatus.FailTask(task, $"Partition scan failed: {ex.Message}");
        throw;
    }
}
```

## UI consumption pattern

In a view model:
- subscribe to `SnapshotChanged`
- read `GetSnapshot()` at initialization
- project to UI-friendly text fields

Current integration reference:
- `Listen2MeRefined.Infrastructure/Mvvm/MainWindow/MainWindowViewModel.cs`

## Best practices

- Use stable `taskKey` values for telemetry and debugging.
- Use meaningful `displayName` for user-facing status.
- Keep `processedDelta` strictly non-negative.
- Prefer one logical task per UX concern; use workers for parallelism inside it.
- Always finalize tasks explicitly (`CompleteTask`/`FailTask`).
- Include failure messages that are short and actionable.

## Common pitfalls

- Registering workers before `StartTask` (invalid handle).
- Forgetting `CompleteTask` after all workers complete.
- Reporting negative deltas (throws `ArgumentOutOfRangeException`).
- Mixing unrelated workflows into one logical task (poor UX signal).

## Current references

- Contract: `Listen2MeRefined.Infrastructure/Services/Contracts/IBackgroundTaskStatusService.cs`
- Service: `Listen2MeRefined.Infrastructure/Services/BackgroundTaskStatusService.cs`
- Models:
  - `Listen2MeRefined.Infrastructure/Services/Models/BackgroundTaskSnapshot.cs`
  - `Listen2MeRefined.Infrastructure/Services/Models/BackgroundTaskItem.cs`
  - `Listen2MeRefined.Infrastructure/Services/Models/TaskProgressKind.cs`
  - `Listen2MeRefined.Infrastructure/Services/Models/BackgroundTaskState.cs`
- Example producer:
  - `Listen2MeRefined.Infrastructure/Services/FolderScannerService.cs`
- Example consumer:
  - `Listen2MeRefined.Infrastructure/Mvvm/MainWindow/MainWindowViewModel.cs`
