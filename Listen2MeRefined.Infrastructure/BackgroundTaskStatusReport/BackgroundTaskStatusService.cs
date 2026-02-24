using Listen2MeRefined.Infrastructure.Services.Contracts;
using Listen2MeRefined.Infrastructure.Services.Models;

namespace Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;

public sealed class BackgroundTaskStatusService : IBackgroundTaskStatusService
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, TrackedTask> _tasks = new();
    private readonly Dictionary<Guid, Guid> _workerTaskMap = new();
    private readonly IAppSettingsReadService _settingsReadService;
    private readonly ILogger _logger;
    private readonly TimeSpan _terminalVisibilityDuration;

    private long _taskSequence;
    private BackgroundTaskItem? _terminalTask;
    private CancellationTokenSource? _clearTerminalCts;
    private BackgroundTaskSnapshot _lastSnapshot = new(false, null, 0);
    private bool? _lastShowMilestones;
    private TaskStatusCountBasis? _lastMilestoneBasis;
    private int? _lastMilestoneInterval;

    public event EventHandler<BackgroundTaskSnapshot>? SnapshotChanged;

    public BackgroundTaskStatusService(
        IAppSettingsReadService settingsReadService,
        ILogger logger,
        TimeSpan? terminalVisibilityDuration = null)
    {
        _settingsReadService = settingsReadService;
        _logger = logger;
        _terminalVisibilityDuration = terminalVisibilityDuration ?? TimeSpan.FromSeconds(5);
    }

    public TaskHandle StartTask(string taskKey, string displayName, TaskProgressKind progressKind, int priority = 0)
    {
        var task = new TrackedTask(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(taskKey) ? "task" : taskKey,
            string.IsNullOrWhiteSpace(displayName) ? "Working..." : displayName,
            progressKind,
            priority,
            Interlocked.Increment(ref _taskSequence),
            DateTimeOffset.UtcNow);

        lock (_sync)
        {
            _tasks[task.Id] = task;
            _logger.Debug("[BackgroundTaskStatusService] Started task {TaskKey} ({TaskId})", task.TaskKey, task.Id);
        }

        PublishSnapshot();
        return new TaskHandle(task.Id);
    }

    public WorkerHandle RegisterWorker(TaskHandle taskHandle, string workerKey, int? totalUnits = null)
    {
        var worker = new TrackedWorker(Guid.NewGuid(), workerKey, totalUnits);

        lock (_sync)
        {
            if (!_tasks.TryGetValue(taskHandle.TaskId, out var task))
            {
                throw new InvalidOperationException($"Task '{taskHandle.TaskId}' was not found.");
            }

            task.Workers[worker.Id] = worker;
            _workerTaskMap[worker.Id] = task.Id;
        }

        PublishSnapshot();
        return new WorkerHandle(taskHandle.TaskId, worker.Id);
    }

    public void ReportWorker(WorkerHandle workerHandle, int processedDelta, int? totalUnits = null, string? message = null)
    {
        if (processedDelta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processedDelta), "Processed delta must be non-negative.");
        }

        lock (_sync)
        {
            if (!TryGetWorker(workerHandle, out var task, out var worker))
            {
                return;
            }

            var previousProcessed = GetProcessedUnits(task);
            var previousTotal = GetTotalUnits(task);
            int? previousRemaining = previousTotal is null
                ? null
                : Math.Max(previousTotal.Value - previousProcessed, 0);

            worker.ProcessedUnits += processedDelta;
            if (totalUnits.HasValue)
            {
                worker.TotalUnits = totalUnits.Value;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                task.Message = message;
            }

            var currentProcessed = GetProcessedUnits(task);
            var currentTotal = GetTotalUnits(task);
            int? currentRemaining = currentTotal is null
                ? null
                : Math.Max(currentTotal.Value - currentProcessed, 0);

            UpdateMilestoneText(task, previousProcessed, previousRemaining, currentProcessed, currentRemaining);
        }

        PublishSnapshot();
    }

    public void CompleteWorker(WorkerHandle workerHandle)
    {
        lock (_sync)
        {
            if (!TryGetWorker(workerHandle, out _, out var worker))
            {
                return;
            }

            worker.IsCompleted = true;
        }

        PublishSnapshot();
    }

    public void FailWorker(WorkerHandle workerHandle, string error)
    {
        lock (_sync)
        {
            if (!TryGetWorker(workerHandle, out var task, out var worker))
            {
                return;
            }

            worker.IsCompleted = true;
            worker.IsFailed = true;
            task.Message = string.IsNullOrWhiteSpace(error) ? "Task failed." : error;
            FailTaskInternal(task, task.Message);
        }

        PublishSnapshot();
    }

    public void CompleteTask(TaskHandle taskHandle, string? message = null)
    {
        lock (_sync)
        {
            if (_tasks.TryGetValue(taskHandle.TaskId, out var task))
            {
                CompleteTaskInternal(task, message);
            }
        }

        PublishSnapshot();
    }

    public void FailTask(TaskHandle taskHandle, string error)
    {
        lock (_sync)
        {
            if (_tasks.TryGetValue(taskHandle.TaskId, out var task))
            {
                FailTaskInternal(task, error);
            }
        }

        PublishSnapshot();
    }

    public BackgroundTaskSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            if (_tasks.Count == 0 && _terminalTask is null)
            {
                return new BackgroundTaskSnapshot(false, null, 0);
            }

            ApplyPresentationSettingChanges();
            return BuildSnapshot();
        }
    }

    public void RefreshSnapshot()
    {
        PublishSnapshot();
    }

    private void PublishSnapshot()
    {
        BackgroundTaskSnapshot snapshot;
        EventHandler<BackgroundTaskSnapshot>? handler;

        lock (_sync)
        {
            if (_tasks.Count == 0 && _terminalTask is null)
            {
                snapshot = new BackgroundTaskSnapshot(false, null, 0);
            }
            else
            {
                ApplyPresentationSettingChanges();
                snapshot = BuildSnapshot();
            }

            if (snapshot == _lastSnapshot)
            {
                return;
            }

            _lastSnapshot = snapshot;
            handler = SnapshotChanged;
        }

        handler?.Invoke(this, snapshot);
    }

    private BackgroundTaskSnapshot BuildSnapshot()
    {
        var activeTasks = _tasks.Values
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Sequence)
            .ToArray();

        if (activeTasks.Length > 0)
        {
            var primary = BuildTaskItem(activeTasks[0], BackgroundTaskState.Running);
            return new BackgroundTaskSnapshot(true, primary, activeTasks.Length - 1);
        }

        if (_terminalTask is not null)
        {
            return new BackgroundTaskSnapshot(true, _terminalTask, 0);
        }

        return new BackgroundTaskSnapshot(false, null, 0);
    }

    private BackgroundTaskItem BuildTaskItem(TrackedTask task, BackgroundTaskState state)
    {
        var processed = GetProcessedUnits(task);
        var total = GetTotalUnits(task);
        var isDeterminate = task.ProgressKind == TaskProgressKind.Determinate && total is > 0;
        var rawPercent = isDeterminate
            ? (int)Math.Clamp(processed * 100L / total!.Value, 0, 100)
            : (int?)null;

        if (state == BackgroundTaskState.Completed && isDeterminate)
        {
            rawPercent = 100;
        }

        var showPercent = _settingsReadService.GetShowTaskPercentage();
        var percentInterval = Math.Clamp((int)_settingsReadService.GetTaskPercentageReportInterval(), 1, 25);
        int? displayPercent = showPercent && rawPercent is not null
            ? (state == BackgroundTaskState.Completed
                ? 100
                : rawPercent.Value - (rawPercent.Value % percentInterval))
            : null;

        var showCount = _settingsReadService.GetShowScanMilestoneCount();
        var countText = showCount ? task.MilestoneText : null;

        return new BackgroundTaskItem(
            task.TaskKey,
            task.DisplayName,
            state,
            isDeterminate,
            processed,
            total,
            displayPercent,
            countText,
            task.Message,
            task.StartedAtUtc);
    }

    private void CompleteTaskInternal(TrackedTask task, string? message)
    {
        task.Message = string.IsNullOrWhiteSpace(message) ? task.Message : message;
        TerminalizeTask(task, BackgroundTaskState.Completed);
    }

    private void FailTaskInternal(TrackedTask task, string? error)
    {
        task.Message = string.IsNullOrWhiteSpace(error) ? "Task failed." : error;
        TerminalizeTask(task, BackgroundTaskState.Failed);
    }

    private void TerminalizeTask(TrackedTask task, BackgroundTaskState terminalState)
    {
        if (!_tasks.Remove(task.Id))
        {
            return;
        }

        foreach (var workerId in task.Workers.Keys)
        {
            _workerTaskMap.Remove(workerId);
        }

        _terminalTask = BuildTaskItem(task, terminalState);
        ScheduleTerminalClear();
    }

    private void ScheduleTerminalClear()
    {
        _clearTerminalCts?.Cancel();
        _clearTerminalCts?.Dispose();

        var cts = new CancellationTokenSource();
        _clearTerminalCts = cts;
        _ = ClearTerminalAfterDelayAsync(cts);
    }

    private async Task ClearTerminalAfterDelayAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_terminalVisibilityDuration, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        lock (_sync)
        {
            if (!ReferenceEquals(_clearTerminalCts, cts))
            {
                return;
            }

            _terminalTask = null;
            _clearTerminalCts.Dispose();
            _clearTerminalCts = null;
        }

        PublishSnapshot();
    }

    private bool TryGetWorker(WorkerHandle workerHandle, out TrackedTask task, out TrackedWorker worker)
    {
        if (!_tasks.TryGetValue(workerHandle.TaskId, out var trackedTask))
        {
            task = null!;
            worker = null!;
            return false;
        }

        if (!_workerTaskMap.TryGetValue(workerHandle.WorkerId, out var mappedTaskId) || mappedTaskId != workerHandle.TaskId)
        {
            task = null!;
            worker = null!;
            return false;
        }

        if (!trackedTask.Workers.TryGetValue(workerHandle.WorkerId, out var trackedWorker))
        {
            task = null!;
            worker = null!;
            return false;
        }

        task = trackedTask;
        worker = trackedWorker;
        return true;
    }

    private void UpdateMilestoneText(
        TrackedTask task,
        int previousProcessed,
        int? previousRemaining,
        int currentProcessed,
        int? currentRemaining)
    {
        if (!_settingsReadService.GetShowScanMilestoneCount())
        {
            task.MilestoneText = null;
            return;
        }

        var interval = Math.Clamp((int)_settingsReadService.GetScanMilestoneInterval(), 5, 500);
        var basis = _settingsReadService.GetScanMilestoneBasis();

        switch (basis)
        {
            case TaskStatusCountBasis.Processed:
            {
                var previousBucket = previousProcessed / interval;
                var currentBucket = currentProcessed / interval;
                if (currentBucket > previousBucket)
                {
                    var latestBoundary = currentBucket * interval;
                    task.MilestoneText = $"{latestBoundary} processed";
                }

                break;
            }
            case TaskStatusCountBasis.Remaining when previousRemaining is not null && currentRemaining is not null:
            {
                var previousBucket = previousRemaining.Value / interval;
                var currentBucket = currentRemaining.Value / interval;
                if (currentBucket < previousBucket)
                {
                    var latestBoundary = (currentBucket + 1) * interval;
                    task.MilestoneText = $"{latestBoundary} remaining";
                }

                break;
            }
        }
    }

    private void ApplyPresentationSettingChanges()
    {
        var showMilestones = _settingsReadService.GetShowScanMilestoneCount();
        var basis = _settingsReadService.GetScanMilestoneBasis();
        var interval = Math.Clamp((int)_settingsReadService.GetScanMilestoneInterval(), 5, 500);

        if (_lastShowMilestones == showMilestones
            && _lastMilestoneBasis == basis
            && _lastMilestoneInterval == interval)
        {
            return;
        }

        _lastShowMilestones = showMilestones;
        _lastMilestoneBasis = basis;
        _lastMilestoneInterval = interval;

        foreach (var task in _tasks.Values)
        {
            task.MilestoneText = null;
        }
    }

    private static int GetProcessedUnits(TrackedTask task)
    {
        return task.Workers.Values.Sum(x => x.ProcessedUnits);
    }

    private static int? GetTotalUnits(TrackedTask task)
    {
        if (task.ProgressKind != TaskProgressKind.Determinate || task.Workers.Count == 0)
        {
            return null;
        }

        var total = 0;
        foreach (var worker in task.Workers.Values)
        {
            if (worker.TotalUnits is null)
            {
                return null;
            }

            total += worker.TotalUnits.Value;
        }

        return total;
    }

    private sealed class TrackedTask
    {
        public TrackedTask(
            Guid id,
            string taskKey,
            string displayName,
            TaskProgressKind progressKind,
            int priority,
            long sequence,
            DateTimeOffset startedAtUtc)
        {
            Id = id;
            TaskKey = taskKey;
            DisplayName = displayName;
            ProgressKind = progressKind;
            Priority = priority;
            Sequence = sequence;
            StartedAtUtc = startedAtUtc;
        }

        public Guid Id { get; }
        public string TaskKey { get; }
        public string DisplayName { get; }
        public TaskProgressKind ProgressKind { get; }
        public int Priority { get; }
        public long Sequence { get; }
        public DateTimeOffset StartedAtUtc { get; }
        public Dictionary<Guid, TrackedWorker> Workers { get; } = new();
        public string? Message { get; set; }
        public string? MilestoneText { get; set; }
    }

    private sealed class TrackedWorker
    {
        public TrackedWorker(Guid id, string workerKey, int? totalUnits)
        {
            Id = id;
            WorkerKey = workerKey;
            TotalUnits = totalUnits;
        }

        public Guid Id { get; }
        public string WorkerKey { get; }
        public int ProcessedUnits { get; set; }
        public int? TotalUnits { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsFailed { get; set; }
    }
}
