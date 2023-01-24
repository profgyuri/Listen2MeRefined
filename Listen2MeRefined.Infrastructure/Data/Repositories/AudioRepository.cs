using Listen2MeRefined.Infrastructure.Data.EntityFramework;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class AudioRepository : RepositoryBase<AudioModel>
{
    public AudioRepository(DataContext dataContext, IDbConnection dbConnection, ILogger logger)
        : base(logger, dataContext, dbConnection)
    { }
}