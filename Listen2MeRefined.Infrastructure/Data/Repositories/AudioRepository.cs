using System.Text;
using Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class AudioRepository : 
    RepositoryBase<AudioModel>,
    IAdvancedDataReader<AdvancedFilter, AudioModel>,
    IFromFolderRemover
{
    public AudioRepository(DataContext dataContext, IDbConnection dbConnection, ILogger logger)
        : base(logger, dataContext, dbConnection)
    { }

    public async Task<IEnumerable<AudioModel>> ReadAsync(IEnumerable<AdvancedFilter> criterias, bool matchAll)
    {
        var filters = criterias as AdvancedFilter[] ?? criterias.ToArray();
        if (filters.Length == 0)
        {
            return Enumerable.Empty<AudioModel>();
        }
        
        var validFields = typeof(AudioModel)
            .GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        var parameters = new DynamicParameters();
        var clauses = new List<string>(filters.Length);

        for (var i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            if (!validFields.Contains(filter.Field))
            {
                throw new InvalidOperationException($"Unsupported filter field: {filter.Field}");
            }

            var parameterName = $"p{i}";
            var sqlOperator = GetSqlOperator(filter.Operator);
            var value = filter.Operator is AdvancedFilterOperator.Contains or AdvancedFilterOperator.NotContains
                ? $"%{filter.Value}%"
                : filter.Value;

            parameters.Add(parameterName, value);
            clauses.Add($"{filter.Field} {sqlOperator} @{parameterName} COLLATE NOCASE");
        }

        var joiner = matchAll ? " AND " : " OR ";
        var whereClause = string.Join(joiner, clauses);
        var tableName = _dataContext.Model.FindEntityType(typeof(AudioModel))!.GetTableName()!;
        var query = new StringBuilder($"SELECT * FROM {tableName} WHERE ")
            .Append(whereClause)
            .ToString();

        return await _dbConnection.QueryAsync<AudioModel>(query, parameters);
        
        static string GetSqlOperator(AdvancedFilterOperator op)
        {
            return op switch
            {
                AdvancedFilterOperator.Equal => "=",
                AdvancedFilterOperator.NotEqual => "<>",
                AdvancedFilterOperator.Contains => "LIKE",
                AdvancedFilterOperator.NotContains => "NOT LIKE",
                AdvancedFilterOperator.GreaterThan => ">",
                AdvancedFilterOperator.LessThan => "<",
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported advanced filter operator")
            };
        }
    }

    public async Task RemoveFromFolderAsync(string folderPath)
    {
        var sql = $"DELETE FROM {_tableName} WHERE Path LIKE @PathPrefix";
        await _dbConnection.ExecuteAsync(sql, new { PathPrefix = $"{folderPath}%" });
    }
}