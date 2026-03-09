using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

public sealed class MusicFolderRepository : RepositoryBase<MusicFolderModel>
{
    public MusicFolderRepository(
        IDbContextFactory<DataContext> dataContextFactory,
        IDbConnection dbConnection,
        ILogger logger)
        : base(logger, dataContextFactory, dbConnection)
    { }
}
