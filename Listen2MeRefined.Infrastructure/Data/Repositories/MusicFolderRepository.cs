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
        _logger.Information("Saving folder to database: {Folder}", data);
        _dataSaver.Save(data);
    }

    /// <inheritdoc />
    public override void Create(IEnumerable<MusicFolderModel> data)
    {
        _logger.Information("Saving multiple folders to database");
        _dataSaver.Save(data);
    }

    /// <inheritdoc />
    public override async Task CreateAsync(MusicFolderModel data)
    {
        _logger.Information("Saving folder to database: {Folder}", data);
        await _dataSaver.SaveAsync(data);
    }

    /// <inheritdoc />
    public override async Task CreateAsync(IEnumerable<MusicFolderModel> data)
    {
        _logger.Information("Saving multiple folders to database");
        await _dataSaver.SaveAsync(data);
    }

    /// <inheritdoc />
    public override IEnumerable<MusicFolderModel> Read()
    {
        return _dataReader.Read<MusicFolderModel>();
    }

    /// <inheritdoc />
    public override IEnumerable<MusicFolderModel> Read(string searchTerm)
    {
        return _dataReader.Read<MusicFolderModel>(searchTerm);
    }

    /// <inheritdoc />
    public override IEnumerable<MusicFolderModel> Read(MusicFolderModel model)
    {
        return _dataReader.Read(model, false);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MusicFolderModel>> ReadAsync()
    {
        return await _dataReader.ReadAsync<MusicFolderModel>();
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MusicFolderModel>> ReadAsync(string searchTerm)
    {
        return await _dataReader.ReadAsync<MusicFolderModel>(searchTerm);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MusicFolderModel>> ReadAsync(MusicFolderModel model)
    {
        return await _dataReader.ReadAsync(model, false);
    }

    /// <inheritdoc />
    public override void Update(MusicFolderModel data)
    {
        _dataUpdater.Update(data);
    }

    /// <inheritdoc />
    public override void Update(IEnumerable<MusicFolderModel> data)
    {
        _dataUpdater.Update(data);
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(MusicFolderModel data)
    {
        await _dataUpdater.UpdateAsync(data);
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(IEnumerable<MusicFolderModel> data)
    {
        await _dataUpdater.UpdateAsync(data);
    }

    /// <inheritdoc />
    public override void Delete(MusicFolderModel data)
    {
        _dataRemover.Remove(data);
    }

    /// <inheritdoc />
    public override void Delete(IEnumerable<MusicFolderModel> data)
    {
        _dataRemover.Remove(data);
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(MusicFolderModel data)
    {
        await _dataRemover.RemoveAsync(data);
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(IEnumerable<MusicFolderModel> data)
    {
        await _dataRemover.RemoveAsync(data);
    }

    /// <inheritdoc />
    public override void DeleteAll()
    {
        _dataRemover.RemoveAll<MusicFolderModel>();
    }

    /// <inheritdoc />
    public override async Task DeleteAllAsync()
    {
        await _dataRemover.RemoveAllAsync<MusicFolderModel>();
    }
    #endregion
}