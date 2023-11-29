namespace Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;

public sealed class PlaylistRepository : RepositoryBase<PlaylistModel>
{
    /// <inheritdoc />
    public PlaylistRepository(DataContext dataContext, IDbConnection dbConnection, ILogger logger)
        : base(logger, dataContext, dbConnection)
    { }
}