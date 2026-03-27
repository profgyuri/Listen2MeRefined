using Listen2MeRefined.Application.Startup;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Listen2MeRefined.Infrastructure.Startup;

public class StartupManager : IStartupManager
{
    private readonly IDatabaseMigrationStartupTask _databaseMigrationStartupTask;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public StartupManager(
        IDatabaseMigrationStartupTask databaseMigrationStartupTask,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _databaseMigrationStartupTask = databaseMigrationStartupTask;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _logger.Debug("[StartupManager] Class initialized");
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.Information("[StartupManager] Starting startup pipeline.");

        await RunStartupTaskWithLoggingAsync(_databaseMigrationStartupTask, ct).ConfigureAwait(false);

        var independentStartupTasks = _serviceProvider
            .GetServices<IStartupTask>()
            .Where(task => task is not IDatabaseMigrationStartupTask)
            .ToArray();

        await Task.WhenAll(
                independentStartupTasks.Select(task => RunStartupTaskWithLoggingAsync(task, ct)))
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
