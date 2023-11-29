namespace Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;

public sealed class MusicFolderRepository : RepositoryBase<MusicFolderModel>
{
    /// <inheritdoc />
    public MusicFolderRepository(DataContext dataContext, IDbConnection dbConnection, ILogger logger)
        : base(logger, dataContext, dbConnection)
    { }
}