namespace Listen2MeRefined.Infrastructure.Data.Dapper;

using global::Dapper;

public class DapperReader : IDataReader
{
    private readonly IDbConnection _connection;

    public DapperReader(IDbConnection connection)
    {
        _connection = connection;
    }

    public IList<T> Read<T>() where T : Model
    {
        var sql = $"SELECT * FROM {typeof(T).Name}s";
        return _connection.Query<T>(sql).ToList();
    }

    public async Task<IList<T>> ReadAsync<T>() where T : Model
    {
        var sql = $"SELECT * FROM {typeof(T).Name}s";
        var result = await _connection.QueryAsync<T>(sql);
        return result.ToList();
    }
}