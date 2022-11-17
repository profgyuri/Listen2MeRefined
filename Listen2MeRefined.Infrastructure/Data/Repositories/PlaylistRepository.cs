namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public class PlaylistRepository : RepositoryBase<PlaylistModel>
{
    /// <inheritdoc />
    public PlaylistRepository(
        IDataReader dataReader,
        IDataSaver dataSaver,
        IDataRemover dataRemover,
        IDataUpdater dataUpdater,
        ILogger logger)
        : base(dataReader, dataSaver, dataRemover,
            dataUpdater, logger)
    {
    }

    #region Overrides of RepositoryBase<PlaylistModel>
    /// <inheritdoc />
    public override void Create(PlaylistModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override void Create(IEnumerable<PlaylistModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override async Task CreateAsync(PlaylistModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task CreateAsync(IEnumerable<PlaylistModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override IEnumerable<PlaylistModel> Read()
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override IEnumerable<PlaylistModel> Read(string searchTerm)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override IEnumerable<PlaylistModel> Read(PlaylistModel model)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<PlaylistModel>> ReadAsync()
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<PlaylistModel>> ReadAsync(string searchTerm)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<PlaylistModel>> ReadAsync(PlaylistModel model)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override void Update(PlaylistModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override void Update(IEnumerable<PlaylistModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(PlaylistModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(IEnumerable<PlaylistModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override void Delete(PlaylistModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override void Delete(IEnumerable<PlaylistModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(PlaylistModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(IEnumerable<PlaylistModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override void DeleteAll()
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override async Task DeleteAllAsync()
    {
        return TODO_IMPLEMENT_ME;
    }
    #endregion
}