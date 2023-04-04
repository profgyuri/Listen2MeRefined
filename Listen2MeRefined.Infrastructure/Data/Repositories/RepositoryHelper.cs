using System.Text;
using Dapper;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

internal static class RepositoryHelper
{
    /// <summary>
    ///     Returns a parameterized query string and a dictionary of parameters using a given search term.
    /// </summary>
    /// <param name="searchTerm">Expression to look for in the fields.</param>
    /// <param name="tableName">Name of the database table.</param>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <returns>An object, that wraps the query to run, and it's dynamic parameter list.</returns>
    internal static ParameterizedQuery GetParameterizedQueryWithSearchTerm<T>(
        string searchTerm,
        string tableName)
        where T : Model
    {
        var properties = GetProperties<T>().ToList();

        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();

        foreach (var property in properties)
        {
            whereClause.Append($"{property} LIKE @{property} OR ");
            whereParams.Add($"{property}", $"%{searchTerm}%");
        }

        whereClause.Length -= 4;

        var sql = $"SELECT * FROM {tableName} WHERE {whereClause}";
        return new ParameterizedQuery(sql, whereParams);
    }
    
     /// <summary>
    ///     Returns a parameterized query string and a dictionary of parameters using the properties of a given model.
    /// </summary>
    /// <param name="model">The not empty properties of this model will be used to build the query.</param>
    /// <param name="exact">If true, the query will look for exact matches, otherwise it will look for partial matches.</param>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <returns>An object, that wraps the query to run, and it's dynamic parameter list.</returns>
    internal static ParameterizedQuery GetParameterizedQueryWithModelProperties<T>(
        T model,
        string tableName,
        bool exact)
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

        whereClause.Length -= 4;

        var sql = $"SELECT * FROM {tableName} WHERE {whereClause}";
        return new ParameterizedQuery(sql, whereParams);
    }
    
    private static IEnumerable<string> GetProperties<T>()
        where T : Model
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
}