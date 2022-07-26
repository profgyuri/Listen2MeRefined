namespace Listen2MeRefined.Infrastructure.Data;

using System.Collections.Generic;

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

    public void Delete(AudioModel data)
    {
        _dataRemover.Remove(data);
    }

    public IList<AudioModel> Read()
    {
        return _dataReader.Read<AudioModel>();
    }

    public void Update(AudioModel data)
    {
        _dataUpdater.Update(data);
    }
}