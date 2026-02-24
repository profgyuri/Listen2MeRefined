using Dapper;
using Dapper.Contrib.Extensions;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public abstract class RepositoryBase<T> : IRepository<T>
    where T: Model
{
    protected readonly ILogger _logger;
    protected readonly IDbContextFactory<DataContext> _dataContextFactory;
    protected readonly IDbConnection _dbConnection;

    protected readonly string _tableName;

    protected RepositoryBase(
        ILogger logger,
        IDbContextFactory<DataContext> dataContextFactory,
        IDbConnection dbConnection)
    {
        _logger = logger;
        _dataContextFactory = dataContextFactory;
        _dbConnection = dbConnection;

        using var context = _dataContextFactory.CreateDbContext();
        _tableName = context.Model.FindEntityType(typeof(T))!.GetTableName()!;
    }
    
    public async Task SaveAsync(T data)
    {
        using var context = _dataContextFactory.CreateDbContext();
        context.AddIfDoesNotExist(data);
        await context.SaveChangesAsync();
    }

    public async Task SaveAsync(IEnumerable<T> list)
    {
        using var context = _dataContextFactory.CreateDbContext();
        await context.AddIfDoesNotExistAsync(list);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<T>> ReadAsync()
    {
        var sql = $"SELECT * FROM {_tableName}";
        return await _dbConnection.QueryAsync<T>(sql);
    }

    public async Task<IEnumerable<T>> ReadAsync(string searchTerm)
    {
        var query = RepositoryHelper.GetParameterizedQueryWithSearchTerm<AudioModel>(searchTerm, _tableName);
        return await _dbConnection.QueryAsync<T>(query.QueryString, query.Parameters);
    }

    public async Task UpdateAsync(T data)
    {
        await _dbConnection.UpdateAsync(data);
    }

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
    }

    public async Task RemoveAsync(T data)
    {
        using var context = _dataContextFactory.CreateDbContext();
        context.Set<T>().Remove(data);
        await context.SaveChangesAsync();
    }

    public async Task RemoveAsync(IEnumerable<T> list)
    {
        using var context = _dataContextFactory.CreateDbContext();
        context.Set<T>().RemoveRange(list);
        await context.SaveChangesAsync();
    }

    public async Task RemoveAllAsync()
    {
        var sql = $"DELETE FROM {_tableName}";
        await _dbConnection.ExecuteAsync(sql);
    }
}
