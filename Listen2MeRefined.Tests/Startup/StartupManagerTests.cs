using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.Infrastructure.Startup.Tasks;
using Serilog;

namespace Listen2MeRefined.Tests.Startup;

public class StartupManagerTests
{
    private static readonly ILogger TestLogger = new LoggerConfiguration().CreateLogger();

    [Fact]
    public async Task StartAsync_RunsDatabaseMigrationBeforeOtherTasks()
    {
        var migrationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowMigrationCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var independentTaskStarted = false;

        var migrationTask = new FakeDatabaseMigrationStartupTask(async _ =>
        {
            migrationStarted.SetResult();
            await allowMigrationCompletion.Task;
        });

        var independentTasks = new IStartupTask[]
        {
            migrationTask,
            new FakeStartupTask(_ =>
            {
                independentTaskStarted = true;
                return Task.CompletedTask;
            })
        };

        var startupManager = new StartupManager(
            migrationTask,
            independentTasks,
            TestLogger);

        var startupRun = startupManager.StartAsync();

        await migrationStarted.Task;
        Assert.False(independentTaskStarted);

        allowMigrationCompletion.SetResult();
        await startupRun;

        Assert.True(independentTaskStarted);
    }

    [Fact]
    public async Task StartAsync_RunsIndependentTasksEvenIfOneFails_ThenThrows()
    {
        var successfulTaskExecuted = false;

        var migrationTask = new FakeDatabaseMigrationStartupTask(_ => Task.CompletedTask);
        var failingTask = new FakeStartupTask(_ => Task.FromException(new InvalidOperationException("boom")));
        var successfulTask = new FakeStartupTask(async _ =>
        {
            await Task.Delay(25);
            successfulTaskExecuted = true;
        });

        var startupManager = new StartupManager(
            migrationTask,
            [migrationTask, failingTask, successfulTask],
            TestLogger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => startupManager.StartAsync());
        Assert.True(successfulTaskExecuted);
    }

    [Fact]
    public async Task StartAsync_HonorsCancellation_AndSkipsIndependentTasks()
    {
        var independentTaskExecuted = false;

        var migrationTask = new FakeDatabaseMigrationStartupTask(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        var startupManager = new StartupManager(
            migrationTask,
            [
                migrationTask,
                new FakeStartupTask(_ =>
                {
                    independentTaskExecuted = true;
                    return Task.CompletedTask;
                })
            ],
            TestLogger);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startupManager.StartAsync(cts.Token));
        Assert.False(independentTaskExecuted);
    }

    private class FakeStartupTask(Func<CancellationToken, Task> runAsync) : IStartupTask
    {
        public Task RunAsync(CancellationToken ct) => runAsync(ct);
    }

    private sealed class FakeDatabaseMigrationStartupTask(Func<CancellationToken, Task> runAsync)
        : FakeStartupTask(runAsync), IDatabaseMigrationStartupTask;
}
