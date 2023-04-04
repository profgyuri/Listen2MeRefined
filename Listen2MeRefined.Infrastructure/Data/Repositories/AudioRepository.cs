using System.Text;
using Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class AudioRepository : 
    RepositoryBase<AudioModel>,
    IAdvancedDataReader<ParameterizedQuery, AudioModel>
{
    public AudioRepository(DataContext dataContext, IDbConnection dbConnection, ILogger logger)
        : base(logger, dataContext, dbConnection)
    { }

    #region Implementation of IAdvancedDataReader<in ParameterizedQuery,AudioModel>
    /// <inheritdoc />
    public async Task<IEnumerable<AudioModel>> ReadAsync(IEnumerable<ParameterizedQuery> criterias, bool matchAll)
    {
        var concatenation = matchAll ? "AND" : "OR";
        var parameterizedQueries = criterias as ParameterizedQuery[] ?? criterias.ToArray();
        var clauses = parameterizedQueries.Select(x => x.QueryString);
        var parameterList = parameterizedQueries.Select(x => x.Parameters);
        var parameters = new DynamicParameters();

        foreach (var param in parameterList)
        {
            parameters.AddDynamicParams(param);
        }

        var builder = new StringBuilder();
        builder.Append($"SELECT * FROM {_dataContext.Model.FindEntityType(typeof(AudioModel))!.GetTableName()!} WHERE ");
        foreach (var clause in clauses)
        {
            builder.Append(clause);
            builder.Append(" COLLATE NOCASE");
            builder.Append($" {concatenation} ");
        }

        builder.Length -= concatenation.Length + 1;
        var query = builder.ToString();
        return await _dbConnection.QueryAsync<AudioModel>(query, parameters);
    }
    #endregion
}