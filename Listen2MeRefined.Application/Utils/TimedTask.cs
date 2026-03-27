namespace Listen2MeRefined.Application.Utils;

/// <summary>
///     Wrapper class for <see cref="PeriodicTimer" />.
///     Accepts an action which will be executed on each timer tick
///     until the timer is stopped.
/// </summary>
public sealed class TimedTask : IAsyncDisposable
{
    private Task? _timerTask;
    private PeriodicTimer? _timer;
    private readonly CancellationTokenSource _cts;

    public TimedTask()
    {
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    ///     Starts the timer.
    /// </summary>
    /// <param name="interval">The interval between each tick.</param>
    /// <param name="action">The action to execute on each tick.</param>
    /// <exception cref="InvalidOperationException">Thrown if the timer is already running.</exception>
    public void Start(
        TimeSpan interval,
        Action action)
    {
        if (_timerTask is not null)
        {
            throw new InvalidOperationException("Task already started");
        }

        _timer = new PeriodicTimer(interval);
        _timerTask = DoWorkAsync(action);
    }

    /// <summary>
    ///     Stops the timer but waits for the current tick to finish.
    /// </summary>
    public async Task StopAsync()
    {
        if (_timerTask is null)
        {
            return;
        }

        await _cts.CancelAsync();
        await _timerTask;
    }
    
    private async Task DoWorkAsync(Action action)
    {
        if (_timer is null)
        {
            return;
        }
        
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                await Task.Run(action);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    /// <summary>
    ///     Stops the timer and disposes the underlying <see cref="PeriodicTimer" />.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _timerTask?.Dispose();
        _timer?.Dispose();
        _cts?.Dispose();
    }
}