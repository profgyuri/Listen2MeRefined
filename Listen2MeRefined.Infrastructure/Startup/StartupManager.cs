using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Infrastructure.Startup.Tasks;

namespace Listen2MeRefined.Infrastructure.Startup;

public sealed class StartupManager
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
        _logger.Debug("[StartupManager] Starting StartAsync...");

        await _databaseMigrationStartupTask.RunAsync(ct).ConfigureAwait(false);

        await Task.WhenAll(_independentStartupTasks.Select(task => task.RunAsync(ct))).ConfigureAwait(false);

        _logger.Debug("[StartupManager] StartAsync completed.");
    }
}
