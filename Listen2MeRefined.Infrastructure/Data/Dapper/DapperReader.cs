using System.Text;

namespace Listen2MeRefined.Infrastructure.Data.Dapper;

using global::Dapper;

public class DapperReader : IDataReader
{
    private readonly IDbConnection _connection;

    public DapperReader(IDbConnection connection)
    {
        _connection = connection;
    }

    public IEnumerable<T> Read<T>() where T : Model
    {
        var sql = $"SELECT * FROM {typeof(T).Name}s";
        return _connection.Query<T>(sql);
    }

    public async Task<IEnumerable<T>> ReadAsync<T>() where T : Model
    {
        var sql = $"SELECT * FROM {typeof(T).Name}s";
        return await _connection.QueryAsync<T>(sql);
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(string searchTerm) where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();
        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();
        
        foreach (var property in properties)
        {
            whereClause.Append($"{property} LIKE @{property} OR ");
            whereParams.Add($"{property}", $"%{searchTerm}%");
        }
        
        whereClause.Remove(whereClause.Length - 4, 4);
        
        var sql = $"SELECT * FROM {typeof(T).Name}s WHERE {whereClause}";
        return _connection.Query<T>(sql, whereParams);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(string searchTerm) where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();
        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();
        
        foreach (var property in properties)
        {
            whereClause.Append($"{property} LIKE @{property} OR ");
            whereParams.Add($"{property}", $"%{searchTerm}%");
        }
        
        whereClause.Remove(whereClause.Length - 4, 4);
        
        var sql = $"SELECT * FROM {typeof(T).Name}s WHERE {whereClause}";
        var result = await _connection.QueryAsync<T>(sql, whereParams);
        return result;
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(T model, bool exact) where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();
        
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
        
        var sql = $"SELECT * FROM {typeof(T).Name}s WHERE {whereClause}";
        return _connection.Query<T>(sql, whereParams);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(T model, bool exact) where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();
        
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
        
        var sql = $"SELECT * FROM {typeof(T).Name}s WHERE {whereClause}";
        return await _connection.QueryAsync<T>(sql, whereParams);
    }
}