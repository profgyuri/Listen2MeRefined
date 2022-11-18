namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public abstract class RepositoryBase<T> : IRepository<T>
    where T : class
{
    protected readonly IDataReader _dataReader;
    protected readonly IDataSaver _dataSaver;
    protected readonly IDataRemover _dataRemover;
    protected readonly IDataUpdater _dataUpdater;
    protected readonly ILogger _logger;

    protected RepositoryBase(
        IDataReader dataReader,
        IDataSaver dataSaver,
        IDataRemover dataRemover,
        IDataUpdater dataUpdater,
        ILogger logger)
    {
        _dataReader = dataReader;
        _dataSaver = dataSaver;
        _dataRemover = dataRemover;
        _dataUpdater = dataUpdater;
        _logger = logger;
    }

    #region Implementation of IRepository<T>
    /// <inheritdoc />
    public abstract void Create(T data);

    /// <inheritdoc />
    public abstract void Create(IEnumerable<T> data);

    /// <inheritdoc />
    public abstract Task CreateAsync(T data);

    /// <inheritdoc />
    public abstract Task CreateAsync(IEnumerable<T> data);

    /// <inheritdoc />
    public abstract IEnumerable<T> Read();

    /// <inheritdoc />
    public abstract IEnumerable<T> Read(string searchTerm);

    /// <inheritdoc />
    public abstract IEnumerable<T> Read(T model);

    /// <inheritdoc />
    public abstract Task<IEnumerable<T>> ReadAsync();

    /// <inheritdoc />
    public abstract Task<IEnumerable<T>> ReadAsync(string searchTerm);

    /// <inheritdoc />
    public abstract Task<IEnumerable<T>> ReadAsync(T model);

    /// <inheritdoc />
    public abstract void Update(T data);

    /// <inheritdoc />
    public abstract void Update(IEnumerable<T> data);

    /// <inheritdoc />
    public abstract Task UpdateAsync(T data);

    /// <inheritdoc />
    public abstract Task UpdateAsync(IEnumerable<T> data);

    /// <inheritdoc />
    public abstract void Delete(T data);

    /// <inheritdoc />
    public abstract void Delete(IEnumerable<T> data);

    /// <inheritdoc />
    public abstract Task DeleteAsync(T data);

    /// <inheritdoc />
    public abstract Task DeleteAsync(IEnumerable<T> data);

    /// <inheritdoc />
    public abstract void DeleteAll();

    /// <inheritdoc />
    public abstract Task DeleteAllAsync();
    #endregion
}