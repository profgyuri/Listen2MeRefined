namespace Listen2MeRefined.Infrastructure.Data.Repositories;
using System.Text;
using global::Dapper;

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
        var properties = GetSearchableProperties<T>().ToList();

        var whereClause = new StringBuilder();
        var whereParams = new DynamicParameters();

        foreach (var property in properties)
        {
            whereClause.Append($"{property} LIKE @{property} OR ");
            whereParams.Add($"{property}", $"%{searchTerm}%");
        }

        whereClause.Remove(whereClause.Length - 4, 4);

        var sql = $"SELECT * FROM {tableName} WHERE {whereClause}";
        return new ParameterizedQuery(sql, whereParams);
    }
    
    private static IEnumerable<string> GetSearchableProperties<T>()
        where T : Model
    {
        return typeof(T)
            .GetProperties()
            .Where(p => 
            {
                if (p.PropertyType != typeof(string) && 
                    p.PropertyType != typeof(int) && 
                    p.PropertyType != typeof(short))
                {
                    return false;
                }

                var ignoredProperties = new[] { "Id", "Display" };
                return !ignoredProperties.Contains(p.Name);
            })
            .Select(p => p.Name);
    }
}