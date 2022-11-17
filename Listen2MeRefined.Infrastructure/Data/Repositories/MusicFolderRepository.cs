namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class MusicFolderRepository : RepositoryBase<MusicFolderModel>
{
    /// <inheritdoc />
    public MusicFolderRepository(
        IDataReader dataReader,
        IDataSaver dataSaver,
        IDataRemover dataRemover,
        IDataUpdater dataUpdater,
        ILogger logger)
        : base(dataReader, dataSaver, dataRemover,
            dataUpdater, logger)
    {
    }

    #region Overrides of RepositoryBase<MusicFolderModel>
    /// <inheritdoc />
    public override void Create(MusicFolderModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override void Create(IEnumerable<MusicFolderModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override async Task CreateAsync(MusicFolderModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task CreateAsync(IEnumerable<MusicFolderModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override IEnumerable<MusicFolderModel> Read()
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override IEnumerable<MusicFolderModel> Read(string searchTerm)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override IEnumerable<MusicFolderModel> Read(MusicFolderModel model)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MusicFolderModel>> ReadAsync()
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MusicFolderModel>> ReadAsync(string searchTerm)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MusicFolderModel>> ReadAsync(MusicFolderModel model)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override void Update(MusicFolderModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override void Update(IEnumerable<MusicFolderModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(MusicFolderModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(IEnumerable<MusicFolderModel> data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override void Delete(MusicFolderModel data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override void Delete(IEnumerable<MusicFolderModel> data)
    {
        TODO_IMPLEMENT_ME();
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(MusicFolderModel data)
    {
        return TODO_IMPLEMENT_ME;
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(IEnumerable<MusicFolderModel> data)
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