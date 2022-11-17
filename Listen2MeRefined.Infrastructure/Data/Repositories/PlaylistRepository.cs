namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public class PlaylistRepository : IRepository<PlaylistModel>
{
    #region Implementation of IRepository<PlaylistModel>
    /// <inheritdoc />
    public void Create(PlaylistModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public void Create(IEnumerable<PlaylistModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public async Task CreateAsync(PlaylistModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public async Task CreateAsync(IEnumerable<PlaylistModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public IEnumerable<PlaylistModel> Read()
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public IEnumerable<PlaylistModel> Read(string searchTerm)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public IEnumerable<PlaylistModel> Read(PlaylistModel model)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlaylistModel>> ReadAsync()
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlaylistModel>> ReadAsync(string searchTerm)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlaylistModel>> ReadAsync(PlaylistModel model)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public void Update(PlaylistModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public void Update(IEnumerable<PlaylistModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PlaylistModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(IEnumerable<PlaylistModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public void Delete(PlaylistModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public void Delete(IEnumerable<PlaylistModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(PlaylistModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<PlaylistModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public void DeleteAll()
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public async Task DeleteAllAsync()
    {
        return TODO_IMPLEMENT_ME;
    }
    #endregion
}