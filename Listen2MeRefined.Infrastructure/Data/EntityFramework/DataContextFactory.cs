using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public sealed class DataContextFactory : IDbContextFactory<DataContext>
{
    public DataContext CreateDbContext() => new();

    public ValueTask<DataContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new DataContext());
}
