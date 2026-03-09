using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;

/// <summary>
/// Tracks and publishes status for long-running background tasks.
/// </summary>
public interface IBackgroundTaskStatusService
{
    /// <summary>
    /// Raised whenever the visible task snapshot changes.
    /// </summary>
    event EventHandler<BackgroundTaskSnapshot>? SnapshotChanged;

    /// <summary>
    /// Starts a logical task and returns its handle.
    /// </summary>
    /// <param name="taskKey">Stable task key used for grouping and diagnostics.</param>
    /// <param name="displayName">User-facing task name.</param>
    /// <param name="progressKind">Whether task progress is determinate or indeterminate.</param>
    /// <param name="priority">Priority used when selecting the primary visible task.</param>
    /// <returns>A handle that identifies the started task.</returns>
    TaskHandle StartTask(string taskKey, string displayName, TaskProgressKind progressKind, int priority = 0);

    /// <summary>
    /// Registers a worker that contributes progress to a logical task.
    /// </summary>
    /// <param name="taskHandle">Parent task handle.</param>
    /// <param name="workerKey">Stable worker key for diagnostics.</param>
    /// <param name="totalUnits">Optional total unit count for this worker.</param>
    /// <returns>A handle that identifies the registered worker.</returns>
    WorkerHandle RegisterWorker(TaskHandle taskHandle, string workerKey, int? totalUnits = null);

    /// <summary>
    /// Reports incremental worker progress and optional message updates.
    /// </summary>
    /// <param name="workerHandle">Worker handle to update.</param>
    /// <param name="processedDelta">Incremental number of processed units.</param>
    /// <param name="totalUnits">Optional updated total units for the worker.</param>
    /// <param name="message">Optional user-facing progress message.</param>
    void ReportWorker(WorkerHandle workerHandle, int processedDelta, int? totalUnits = null, string? message = null);

    /// <summary>
    /// Marks a worker as completed.
    /// </summary>
    /// <param name="workerHandle">Worker handle to complete.</param>
    void CompleteWorker(WorkerHandle workerHandle);

    /// <summary>
    /// Marks a worker as failed and records its error message.
    /// </summary>
    /// <param name="workerHandle">Worker handle to fail.</param>
    /// <param name="error">Failure message.</param>
    void FailWorker(WorkerHandle workerHandle, string error);

    /// <summary>
    /// Marks a logical task as completed.
    /// </summary>
    /// <param name="taskHandle">Task handle to complete.</param>
    /// <param name="message">Optional completion message.</param>
    void CompleteTask(TaskHandle taskHandle, string? message = null);

    /// <summary>
    /// Marks a logical task as failed.
    /// </summary>
    /// <param name="taskHandle">Task handle to fail.</param>
    /// <param name="error">Failure message.</param>
    void FailTask(TaskHandle taskHandle, string error);

    /// <summary>
    /// Gets the current task snapshot.
    /// </summary>
    /// <returns>The current visible task snapshot.</returns>
    BackgroundTaskSnapshot GetSnapshot();

    /// <summary>
    /// Recomputes and republishes the current snapshot.
    /// </summary>
    void RefreshSnapshot();
}
