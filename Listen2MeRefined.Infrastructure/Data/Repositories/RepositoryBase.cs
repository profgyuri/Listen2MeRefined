using Dapper;
using Dapper.Contrib.Extensions;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public abstract class RepositoryBase<T> : IRepository<T>
    where T: Model
{
    protected readonly ILogger _logger;
    protected readonly DataContext _dataContext;
    protected readonly IDbConnection _dbConnection;

    private readonly string _tableName;

    protected RepositoryBase(ILogger logger, DataContext dataContext, IDbConnection dbConnection)
    {
        _logger = logger;
        _dataContext = dataContext;
        _dbConnection = dbConnection;
        
        _tableName = _dataContext.Model.FindEntityType(typeof(T))!.GetTableName()!;
    }
    
    #region Implementation of IDataSaver<in T>
    /// <inheritdoc />
    public async Task SaveAsync(T data)
    {
        _dataContext.AddIfDoesNotExist(data);
        await _dataContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SaveAsync(IEnumerable<T> list)
    {
        await _dataContext.AddIfDoesNotExistAsync(list);
        await _dataContext.SaveChangesAsync();
    }
    #endregion

    #region Implementation of IDataReader<T>
    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync()
    {
        var sql = $"SELECT * FROM {_tableName}";
        return await _dbConnection.QueryAsync<T>(sql);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync(string searchTerm)
    {
        var query = RepositoryHelper.GetParameterizedQueryWithSearchTerm<AudioModel>(searchTerm, _tableName);
        return await _dbConnection.QueryAsync<T>(query.QueryString, query.Parameters);
    }
    #endregion

    #region Implementation of IDataUpdater<in T>
    /// <inheritdoc />
    public async Task UpdateAsync(T data)
    {
        await _dbConnection.UpdateAsync(data);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(IEnumerable<T> list)
    {
        try
        {
            await _dbConnection.UpdateAsync(list);
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
        }

        await _dataContext.SaveChangesAsync();
    }
    #endregion

    #region Implementation of IDataRemover<T>
    /// <inheritdoc />
    public async Task RemoveAsync(T data)
    {
        _dataContext.Set<T>().Remove(data);
        await _dataContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveAsync(IEnumerable<T> list)
    {
        _dataContext.Set<T>().RemoveRange(list);
        await _dataContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveAllAsync()
    {
        var sql = $"DELETE FROM {_tableName}";
        await _dbConnection.ExecuteAsync(sql);
    }
    #endregion
}