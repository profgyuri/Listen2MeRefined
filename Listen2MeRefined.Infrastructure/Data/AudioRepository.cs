namespace Listen2MeRefined.Infrastructure.Data;

public sealed class AudioRepository : IRepository<AudioModel>
{
    private readonly IDataReader _dataReader;
    private readonly IDataSaver _dataSaver;
    private readonly IDataRemover _dataRemover;
    private readonly IDataUpdater _dataUpdater;
    private readonly ILogger _logger;

    public AudioRepository(IDataReader dataReader, IDataSaver dataSaver, IDataRemover dataRemover, IDataUpdater dataUpdater, ILogger logger)
    {
        _dataReader = dataReader;
        _dataSaver = dataSaver;
        _dataRemover = dataRemover;
        _dataUpdater = dataUpdater;
        _logger = logger;
    }

    #region IDataSaver
    public void Create(AudioModel data)
    {
        _logger.Information("Saving audio to database: {Audio}", data.Display);
        _dataSaver.Save(data);
    }

    public void Create(IEnumerable<AudioModel> data)
    {
        data.TryGetNonEnumeratedCount(out var count);
        _logger.Information("Saving audio list to database: {Count}", count);
        _dataSaver.Save(data);
    }

    public async Task CreateAsync(AudioModel data)
    {
        _logger.Information("Saving audio to database: {Audio}", data.Display);
        await _dataSaver.SaveAsync(data);
    }

    public async Task CreateAsync(IEnumerable<AudioModel> data)
    {
        data.TryGetNonEnumeratedCount(out var count);
        _logger.Information("Saving audio list to database: {Count}", count);
        await _dataSaver.SaveAsync(data);
    }
    #endregion

    #region IDataRemover
    public void Delete(AudioModel data)
    {
        _dataRemover.Remove(data);
    }

    public void Delete(IEnumerable<AudioModel> data)
    {
        _dataRemover.Remove(data);
    }

    public async Task DeleteAsync(AudioModel data)
    {
        await _dataRemover.RemoveAsync(data);
    }

    public async Task DeleteAsync(IEnumerable<AudioModel> data)
    {
        await _dataRemover.RemoveAsync(data);
    }
    
    public void DeleteAll()
    {
        _dataRemover.RemoveAll<AudioModel>();
    }
    
    public async Task DeleteAllAsync()
    {
        await _dataRemover.RemoveAllAsync<AudioModel>();
    }
    #endregion

    #region IDataReader
    public IEnumerable<AudioModel> Read()
    {
        return _dataReader.Read<AudioModel>();
    }

    public async Task<IEnumerable<AudioModel>> ReadAsync()
    {
        return await _dataReader.ReadAsync<AudioModel>();
    }
    
    public IEnumerable<AudioModel> Read(string searchTerm)
    {
        return _dataReader.Read<AudioModel>(searchTerm);
    }
    
    public async Task<IEnumerable<AudioModel>> ReadAsync(string searchTerm)
    {
        return await _dataReader.ReadAsync<AudioModel>(searchTerm);
    }
    
    public IEnumerable<AudioModel> Read(AudioModel model)
    {
        return _dataReader.Read(model, false);
    }
    
    public async Task<IEnumerable<AudioModel>> ReadAsync(AudioModel model)
    {
        return await _dataReader.ReadAsync(model, false);
    }
    #endregion

    #region IDataUpdater
    public void Update(AudioModel data)
    {
        _dataUpdater.Update(data);
    }

    public void Update(IEnumerable<AudioModel> data)
    {
        _dataUpdater.Update(data);
    }

    public async Task UpdateAsync(AudioModel data)
    {
        await _dataUpdater.UpdateAsync(data);
    }

    public async Task UpdateAsync(IEnumerable<AudioModel> data)
    {
        await _dataUpdater.UpdateAsync(data);
    }
    #endregion
}