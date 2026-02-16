using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class DatabaseMigrationStartupTask(DataContext dataContext, ILogger logger) : IDatabaseMigrationStartupTask
{
    public async Task RunAsync(CancellationToken ct)
    {
        await dataContext.Database.MigrateAsync(ct).ConfigureAwait(false);
        logger.Information("[DatabaseMigrationStartupTask] Database migration completed.");
    }
}