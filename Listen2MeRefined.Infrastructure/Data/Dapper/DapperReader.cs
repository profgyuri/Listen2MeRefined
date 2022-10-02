using System.Text;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using global::Dapper;

namespace Listen2MeRefined.Infrastructure.Data.Dapper;

public sealed class DapperReader : IDataReader
{
    private readonly IDbConnection _connection;
    private readonly DataContext _dataContext;

    public DapperReader(IDbConnection connection, DataContext dataContext)
    {
        _connection = connection;
        _dataContext = dataContext;
    }

    #region Synchronous overloads
    /// <inheritdoc />
    public IEnumerable<T> Read<T>() where T : Model
    {
        var sql = $"SELECT * FROM {GetTableName<T>()}";
        return _connection.Query<T>(sql);
    }
    
    /// <inheritdoc />
    public IEnumerable<T> Read<T>(string searchTerm) where T : Model
    {
        var query = GetParameterizedQueryWithSearchTerm<T>(searchTerm);
        return _connection.Query<T>(query.QueryString, query.Parameters);
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(T model, bool exact) where T : Model
    {
        var query = GetParameterizedQueryWithModelProperties(model, exact);
        return _connection.Query<T>(query.QueryString, query.Parameters);
    }
    #endregion

    #region Async overloads
    public async Task<IEnumerable<T>> ReadAsync<T>() where T : Model
    {
        var sql = $"SELECT * FROM {GetTableName<T>()}";
        return await _connection.QueryAsync<T>(sql);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(string searchTerm) where T : Model
    {
        var query = GetParameterizedQueryWithSearchTerm<T>(searchTerm);
        var result = await _connection.QueryAsync<T>(query.QueryString, query.Parameters);
        return result ?? new List<T>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(T model, bool exact) where T : Model
    {
        var query = GetParameterizedQueryWithModelProperties(model, exact);
        return await _connection.QueryAsync<T>(query.QueryString, query.Parameters);
    }
    #endregion

    #region Private helpers
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
    
    private static IEnumerable<string> GetProperties<T>() where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .Where(p =>
            {
                var ignoredProperties = new HashSet<string> {"Id", "Display"};
                return !ignoredProperties.Contains(p);
            });

        return properties;
    }
    
    /// <summary>
    ///     Returns a parameterized query string and a dictionary of parameters using a given search term.
    /// </summary>
    /// <param name="searchTerm">Expression to look for in the fields.</param>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <returns>An object, that wraps the query to run, and it's dynamic parameter list.</returns>
    private ParameterizedQuery GetParameterizedQueryWithSearchTerm<T>(string searchTerm) where T : Model
    {
        var properties = GetProperties<T>().ToList();

        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();

        foreach (var property in properties)
        {
            whereClause.Append($"{property} LIKE @{property} OR ");
            whereParams.Add($"{property}", $"%{searchTerm}%");
        }

        whereClause.Remove(whereClause.Length - 4, 4);

        var sql = $"SELECT * FROM {GetTableName<T>()} WHERE {whereClause}";
        return new ParameterizedQuery(sql, whereParams);
    }
    
    /// <summary>
    ///     Returns a parameterized query string and a dictionary of parameters using the properties of a given model.
    /// </summary>
    /// <param name="model">The not empty properties of this model will be used to build the query.</param>
    /// <param name="exact">If true, the query will look for exact matches, otherwise it will look for partial matches.</param>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <returns>An object, that wraps the query to run, and it's dynamic parameter list.</returns>
    private ParameterizedQuery GetParameterizedQueryWithModelProperties<T>(T model, bool exact)
        where T : Model
    {
        var properties = GetProperties<T>().ToList();

        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();

        foreach (var property in properties)
        {
            var propertyValue = typeof(T).GetProperty(property)?.GetValue(model);
            var isPropertyValueInvalid = 
                propertyValue == null
                || typeof(T).GetProperty(property) == default
                || propertyValue.ToString() == string.Empty;
            if (isPropertyValueInvalid)
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
        return new ParameterizedQuery(sql, whereParams);
    }
    #endregion
}