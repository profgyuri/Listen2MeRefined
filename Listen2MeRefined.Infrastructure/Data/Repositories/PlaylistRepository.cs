using Microsoft.EntityFrameworkCore;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class PlaylistRepository : RepositoryBase<PlaylistModel>
{
    public PlaylistRepository(
        DataContext dataContext, 
        IDbConnection dbConnection, 
        ILogger logger) : base(logger, dataContext, dbConnection)
    {
    }
    
    public override async Task<IEnumerable<PlaylistModel>> ReadAsync()
    {
        return await _dataContext.Playlists
            .Include(p => p.Songs)
            .ToListAsync();
    }

    public override async Task<IEnumerable<PlaylistModel>> ReadAsync(string searchTerm)
    {
        return await _dataContext.Playlists
            .Include(p => p.Songs)
            .Where(p => p.Name.Contains(searchTerm))
            .ToListAsync();
    }
}