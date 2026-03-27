using Listen2MeRefined.Application.Utils;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

/// <summary>
/// Schedules debounced waveform resize operations and keeps only the latest request.
/// </summary>
public sealed class WaveformResizeScheduler : IWaveformResizeScheduler
{
    private static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(120);

    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly TimeSpan _debounce;
    private readonly object _sync = new();
    private CancellationTokenSource? _currentCancellationSource;
    private Task _pendingTask = Task.CompletedTask;
    private int _requestId;

    public WaveformResizeScheduler(
        TimeSpan? debounce = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        _debounce = debounce ?? DefaultDebounce;
        _delayAsync = delayAsync ?? Task.Delay;
    }

    /// <summary>
    /// Gets the most recently scheduled resize task.
    /// </summary>
    public Task PendingTask
    {
        get
        {
            lock (_sync)
            {
                return _pendingTask;
            }
        }
    }

    /// <summary>
    /// Schedules a debounced resize operation and cancels any previously pending operation.
    /// </summary>
    /// <param name="resizeOperation">The resize operation to execute after debounce.</param>
    /// <param name="externalToken">A token that can cancel the scheduled operation from outside.</param>
    /// <returns>A task that completes when the latest scheduled operation completes.</returns>
    public Task ScheduleResizeAsync(Func<CancellationToken, Task> resizeOperation, CancellationToken externalToken = default)
    {
        ArgumentNullException.ThrowIfNull(resizeOperation);

        var requestId = Interlocked.Increment(ref _requestId);
        var localCts = externalToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(externalToken)
            : new CancellationTokenSource();

        CancellationTokenSource? previous;
        lock (_sync)
        {
            previous = _currentCancellationSource;
            _currentCancellationSource = localCts;
            _pendingTask = ExecuteAsync(requestId, localCts.Token, resizeOperation);
        }

        previous?.Cancel();
        previous?.Dispose();

        return PendingTask;
    }

    /// <summary>
    /// Cancels the currently pending resize operation, if one exists.
    /// </summary>
    public void CancelPending()
    {
        CancellationTokenSource? toCancel;
        lock (_sync)
        {
            toCancel = _currentCancellationSource;
            _currentCancellationSource = null;
        }

        toCancel?.Cancel();
        toCancel?.Dispose();
    }

    private async Task ExecuteAsync(
        int requestId,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task> resizeOperation)
    {
        try
        {
            await _delayAsync(_debounce, cancellationToken).ConfigureAwait(false);
            if (requestId != Volatile.Read(ref _requestId) || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await resizeOperation(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Intentionally ignored for canceled resize requests.
        }
    }
}
