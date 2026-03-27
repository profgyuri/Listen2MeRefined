using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class DatabaseMigrationStartupTask(
    IDbContextFactory<DataContext> dataContextFactory,
    ILogger logger) : IDatabaseMigrationStartupTask
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using var dataContext = await dataContextFactory.CreateDbContextAsync(ct);
        await dataContext.Database.MigrateAsync(ct).ConfigureAwait(false);
        logger.Information("[DatabaseMigrationStartupTask] Database migration completed.");
    }
}
