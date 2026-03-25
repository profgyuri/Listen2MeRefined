using Listen2MeRefined.Application.Startup;
using System.Diagnostics;

namespace Listen2MeRefined.Infrastructure.Startup;

public class StartupManager : IStartupManager
{
    private readonly IDatabaseMigrationStartupTask _databaseMigrationStartupTask;
    private readonly IReadOnlyCollection<IStartupTask> _independentStartupTasks;
    private readonly ILogger _logger;

    public StartupManager(
        IDatabaseMigrationStartupTask databaseMigrationStartupTask,
        IEnumerable<IStartupTask> startupTasks,
        ILogger logger)
    {
        _databaseMigrationStartupTask = databaseMigrationStartupTask;
        _independentStartupTasks = startupTasks
            .Where(task => task is not IDatabaseMigrationStartupTask)
            .ToArray();
        _logger = logger;

        _logger.Debug("[StartupManager] Class initialized");
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.Information("[StartupManager] Starting startup pipeline.");

        await RunStartupTaskWithLoggingAsync(_databaseMigrationStartupTask, ct).ConfigureAwait(false);

        await Task.WhenAll(
                _independentStartupTasks.Select(task => RunStartupTaskWithLoggingAsync(task, ct)))
            .ConfigureAwait(false);

        _logger.Information("[StartupManager] Startup pipeline completed.");
    }

    private async Task RunStartupTaskWithLoggingAsync(IStartupTask task, CancellationToken ct)
    {
        var taskName = task.GetType().Name;
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.Information(
            "[StartupManager] Startup task starting. TaskName={TaskName} StartedAtUtc={StartedAtUtc}",
            taskName,
            startedAtUtc);

        try
        {
            await task.RunAsync(ct).ConfigureAwait(false);
            stopwatch.Stop();

            _logger.Information(
                "[StartupManager] Startup task finished. TaskName={TaskName} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                taskName,
                "Success",
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.Warning(
                "[StartupManager] Startup task finished. TaskName={TaskName} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                taskName,
                "Canceled",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.Error(
                ex,
                "[StartupManager] Startup task finished. TaskName={TaskName} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                taskName,
                "Failure",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
