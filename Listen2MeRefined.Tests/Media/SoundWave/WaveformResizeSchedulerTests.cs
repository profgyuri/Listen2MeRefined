using Listen2MeRefined.Infrastructure.Media.SoundWave;

namespace Listen2MeRefined.Tests.Media.SoundWave;

public sealed class WaveformResizeSchedulerTests
{
    [Fact]
    public async Task ScheduleResizeAsync_RapidCalls_ExecutesOnlyLatestOperation()
    {
        var delayCallCount = 0;
        var scheduler = new WaveformResizeScheduler(
            debounce: TimeSpan.FromMilliseconds(1),
            delayAsync: (_, ct) =>
            {
                var call = Interlocked.Increment(ref delayCallCount);
                return call == 1
                    ? Task.Delay(Timeout.Infinite, ct)
                    : Task.CompletedTask;
            });

        var firstExecutionCount = 0;
        var secondExecutionCount = 0;

        _ = scheduler.ScheduleResizeAsync(_ =>
        {
            Interlocked.Increment(ref firstExecutionCount);
            return Task.CompletedTask;
        });

        await scheduler.ScheduleResizeAsync(_ =>
        {
            Interlocked.Increment(ref secondExecutionCount);
            return Task.CompletedTask;
        });

        Assert.Equal(0, Volatile.Read(ref firstExecutionCount));
        Assert.Equal(1, Volatile.Read(ref secondExecutionCount));
    }

    [Fact]
    public async Task ScheduleResizeAsync_SetsPendingTask()
    {
        var scheduler = new WaveformResizeScheduler(TimeSpan.Zero);
        Task? observedTask = null;

        observedTask = scheduler.ScheduleResizeAsync(_ => Task.CompletedTask);
        await observedTask;

        Assert.NotNull(observedTask);
        Assert.Same(observedTask, scheduler.PendingTask);
        Assert.True(scheduler.PendingTask.IsCompleted);
    }

    [Fact]
    public async Task CancelPending_BeforeDebounce_DoesNotExecuteOperation()
    {
        var scheduler = new WaveformResizeScheduler(
            debounce: TimeSpan.FromMilliseconds(20),
            delayAsync: (_, ct) => Task.Delay(Timeout.Infinite, ct));
        var executionCount = 0;

        _ = scheduler.ScheduleResizeAsync(_ =>
        {
            Interlocked.Increment(ref executionCount);
            return Task.CompletedTask;
        });

        scheduler.CancelPending();
        await scheduler.PendingTask;

        Assert.Equal(0, Volatile.Read(ref executionCount));
    }
}
