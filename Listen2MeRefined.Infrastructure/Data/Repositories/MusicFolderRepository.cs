using Listen2MeRefined.Infrastructure.Data.EntityFramework;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class MusicFolderRepository : RepositoryBase<MusicFolderModel>
{
    /// <inheritdoc />
    public MusicFolderRepository(DataContext dataContext, IDbConnection dbConnection, ILogger logger)
        : base(logger, dataContext, dbConnection)
    { }
}