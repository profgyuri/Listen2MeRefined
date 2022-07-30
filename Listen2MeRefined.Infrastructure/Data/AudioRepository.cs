namespace Listen2MeRefined.Infrastructure.Data;

using System.Collections.Generic;
using System.Threading.Tasks;

public class AudioRepository : IRepository<AudioModel>
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

    public void Create(AudioModel data)
    {
        _dataSaver.Save(data);
    }

    public void Create(IList<AudioModel> data)
    {
        _dataSaver.Save(data);
    }

    public async Task CreateAsync(AudioModel data)
    {
        await _dataSaver.SaveAsync(data);
    }

    public async Task CreateAsync(IList<AudioModel> data)
    {
        await _dataSaver.SaveAsync(data);
    }

    public void Delete(AudioModel data)
    {
        _dataRemover.Remove(data);
    }

    public void Delete(IList<AudioModel> data)
    {
        _dataRemover.Remove(data);
    }

    public async Task DeleteAsync(AudioModel data)
    {
        await _dataRemover.RemoveAsync(data);
    }

    public async Task DeleteAsync(IList<AudioModel> data)
    {
        await _dataRemover.RemoveAsync(data);
    }

    public IList<AudioModel> Read()
    {
        return _dataReader.Read<AudioModel>();
    }

    public async Task<IList<AudioModel>> ReadAsync()
    {
        return await _dataReader.ReadAsync<AudioModel>();
    }

    public void Update(AudioModel data)
    {
        _dataUpdater.Update(data);
    }

    public void Update(IList<AudioModel> data)
    {
        _dataUpdater.Update(data);
    }

    public async Task UpdateAsync(AudioModel data)
    {
        await _dataUpdater.UpdateAsync(data);
    }

    public async Task UpdateAsync(IList<AudioModel> data)
    {
        await _dataUpdater.UpdateAsync(data);
    }
}