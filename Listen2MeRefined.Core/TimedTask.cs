namespace Listen2MeRefined.Core;

public sealed class TimedTask : IAsyncDisposable
{
    private Task? _timerTask;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts;
    
    public TimedTask(TimeSpan interval)
    {
        _timer = new PeriodicTimer(interval);
        _cts = new CancellationTokenSource();
    }
    
    public void Start(Action action)
    {
        _timerTask ??= DoWorkAsync(action);
    }
    
    private async Task DoWorkAsync(Action action)
    {
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

    public async Task StopAsync()
    {
        if (_timerTask is null)
        {
            return;
        }
        
        _cts.Cancel();
        await _timerTask;
        _cts.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}