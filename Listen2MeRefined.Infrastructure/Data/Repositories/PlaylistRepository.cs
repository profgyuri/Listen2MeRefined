using Listen2MeRefined.Infrastructure.Data.EntityFramework;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class PlaylistRepository : RepositoryBase<PlaylistModel>
{
    /// <inheritdoc />
    public PlaylistRepository(DataContext dataContext, IDbConnection dbConnection, ILogger logger)
        : base(logger, dataContext, dbConnection)
    { }
}