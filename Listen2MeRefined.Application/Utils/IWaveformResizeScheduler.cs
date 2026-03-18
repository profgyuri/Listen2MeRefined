namespace Listen2MeRefined.Application.Utils;

/// <summary>
/// Schedules debounced waveform resize redraw operations.
/// </summary>
public interface IWaveformResizeScheduler
{
    /// <summary>
    /// Gets the most recently scheduled resize task.
    /// </summary>
    Task PendingTask { get; }

    /// <summary>
    /// Schedules a debounced resize operation and cancels any previously pending operation.
    /// </summary>
    /// <param name="resizeOperation">The resize operation to execute after debounce.</param>
    /// <param name="externalToken">A token that can cancel the scheduled operation from outside.</param>
    /// <returns>A task that completes when the latest scheduled operation completes.</returns>
    Task ScheduleResizeAsync(Func<CancellationToken, Task> resizeOperation, CancellationToken externalToken = default);

    /// <summary>
    /// Cancels the currently pending resize operation, if one exists.
    /// </summary>
    void CancelPending();
}
