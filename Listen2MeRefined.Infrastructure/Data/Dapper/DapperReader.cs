using System.Text;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.Dapper;

using global::Dapper;

public class DapperReader : IDataReader
{
    private readonly IDbConnection _connection;
    private readonly DataContext _dataContext;

    public DapperReader(IDbConnection connection, DataContext dataContext)
    {
        _connection = connection;
        _dataContext = dataContext;
    }

    public IEnumerable<T> Read<T>() where T : Model
    {
        var sql = $"SELECT * FROM {GetTableName<T>()}";
        return _connection.Query<T>(sql);
    }

    public async Task<IEnumerable<T>> ReadAsync<T>() where T : Model
    {
        var sql = $"SELECT * FROM {GetTableName<T>()}";
        return await _connection.QueryAsync<T>(sql);
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(string searchTerm) where T : Model
    {
        var properties = GetProperties<T>();
        
        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();
        
        foreach (var property in properties)
        {
            whereClause.Append($"{property} LIKE @{property} OR ");
            whereParams.Add($"{property}", $"%{searchTerm}%");
        }
        
        whereClause.Remove(whereClause.Length - 4, 4);
        
        var sql = $"SELECT * FROM {GetTableName<T>()} WHERE {whereClause}";
        return _connection.Query<T>(sql, whereParams);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(string searchTerm) where T : Model
    {
        var properties = GetProperties<T>();
        
        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();
        
        foreach (var property in properties)
        {
            whereClause.Append($"{property} LIKE @{property} OR ");
            whereParams.Add($"{property}", $"%{searchTerm}%");
        }
        
        whereClause.Remove(whereClause.Length - 4, 4);
        
        var sql = $"SELECT * FROM {GetTableName<T>()} WHERE {whereClause}";
        var result = await _connection.QueryAsync<T>(sql, whereParams);
        return result ?? new List<T>();
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(T model, bool exact) where T : Model
    {
        var properties = GetProperties<T>();
        
        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();
        
        foreach (var property in properties)
        {
            var propertyValue = typeof(T).GetProperty(property)?.GetValue(model);
            if (propertyValue == null 
                || typeof(T).GetProperty(property) == default 
                || propertyValue.ToString() == string.Empty)
            {
                continue;
            }
            
            if (exact)
            {
                whereClause.Append($"{property} = @{property} AND ");
                whereParams.Add($"{property}", propertyValue);
            }
            else
            {
                whereClause.Append($"{property} LIKE @{property} AND ");
                whereParams.Add($"{property}", $"%{propertyValue}%");
            }
        }
        
        whereClause.Remove(whereClause.Length - 4, 4);
        
        var sql = $"SELECT * FROM {GetTableName<T>()} WHERE {whereClause}";
        return _connection.Query<T>(sql, whereParams);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(T model, bool exact) where T : Model
    {
        var properties = GetProperties<T>();
        
        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();
        
        foreach (var property in properties)
        {
            var propertyValue = typeof(T).GetProperty(property)?.GetValue(model);
            if (propertyValue == null 
                || typeof(T).GetProperty(property) == default 
                || propertyValue.ToString() == string.Empty)
            {
                continue;
            }
            
            if (exact)
            {
                whereClause.Append($"{property} = @{property} AND ");
                whereParams.Add($"{property}", propertyValue);
            }
            else
            {
                whereClause.Append($"{property} LIKE @{property} AND ");
                whereParams.Add($"{property}", $"%{propertyValue}%");
            }
        }
        
        whereClause.Remove(whereClause.Length - 4, 4);
        
        var sql = $"SELECT * FROM {GetTableName<T>()} WHERE {whereClause}";
        return await _connection.QueryAsync<T>(sql, whereParams);
    }
    
    /// <summary>
    ///     Removes "model" from the end of the type name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private string GetTableName<T>() where T : Model
    {
        var result = _dataContext.Model.FindEntityType(typeof(T))!.GetTableName()!;
        return result;
    }
    
    private IEnumerable<string> GetProperties<T>() where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .Where(p => p != "Display" && p != "Id");

        return properties;
    }
}