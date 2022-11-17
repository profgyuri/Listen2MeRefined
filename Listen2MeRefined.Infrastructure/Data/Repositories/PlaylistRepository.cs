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
        _logger.Information("Creating playlist '{Playlist}' in database", data.Name);
        _dataSaver.Save(data);
    }

    /// <inheritdoc />
    public override void Create(IEnumerable<PlaylistModel> data)
    {
        _logger.Information("Creating multiple playlists in database");
        _dataSaver.Save(data);
    }

    /// <inheritdoc />
    public override async Task CreateAsync(PlaylistModel data)
    {
        _logger.Information("Creating playlist '{Playlist}' in database", data.Name);
        await _dataSaver.SaveAsync(data);
    }

    /// <inheritdoc />
    public override async Task CreateAsync(IEnumerable<PlaylistModel> data)
    {
        _logger.Information("Creating multiple playlists in database");
        await _dataSaver.SaveAsync(data);
    }

    /// <inheritdoc />
    public override IEnumerable<PlaylistModel> Read()
    {
        return _dataReader.Read<PlaylistModel>();
    }

    /// <inheritdoc />
    public override IEnumerable<PlaylistModel> Read(string searchTerm)
    {
        return _dataReader.Read<PlaylistModel>(searchTerm);
    }

    /// <inheritdoc />
    public override IEnumerable<PlaylistModel> Read(PlaylistModel model)
    {
        return _dataReader.Read(model, false);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<PlaylistModel>> ReadAsync()
    {
        return await _dataReader.ReadAsync<PlaylistModel>();
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<PlaylistModel>> ReadAsync(string searchTerm)
    {
        return await _dataReader.ReadAsync<PlaylistModel>(searchTerm);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<PlaylistModel>> ReadAsync(PlaylistModel model)
    {
        return await _dataReader.ReadAsync(model, false);
    }

    /// <inheritdoc />
    public override void Update(PlaylistModel data)
    {
        _dataUpdater.Update(data);
    }

    /// <inheritdoc />
    public override void Update(IEnumerable<PlaylistModel> data)
    {
        _dataUpdater.Update(data);
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(PlaylistModel data)
    {
        await _dataUpdater.UpdateAsync(data);
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(IEnumerable<PlaylistModel> data)
    {
        await _dataUpdater.UpdateAsync(data);
    }

    /// <inheritdoc />
    public override void Delete(PlaylistModel data)
    {
        _dataRemover.Remove(data);
    }

    /// <inheritdoc />
    public override void Delete(IEnumerable<PlaylistModel> data)
    {
        _dataRemover.Remove(data);
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(PlaylistModel data)
    {
        await _dataRemover.RemoveAsync(data);
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(IEnumerable<PlaylistModel> data)
    {
        await _dataRemover.RemoveAsync(data);
    }

    /// <inheritdoc />
    public override void DeleteAll()
    {
        _dataRemover.RemoveAll<PlaylistModel>();
    }

    /// <inheritdoc />
    public override async Task DeleteAllAsync()
    {
        await _dataRemover.RemoveAllAsync<PlaylistModel>();
    }
    #endregion
}