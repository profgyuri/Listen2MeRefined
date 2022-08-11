namespace Listen2MeRefined.Infrastructure.Data.Dapper;

using global::Dapper;

public class DapperRemover : IDataRemover
{
    private readonly IDbConnection _connection;

    public DapperRemover(IDbConnection connection)
    {
        _connection = connection;
    }

    public void Remove<T>(T data) where T : Model
    {
        var sql = $"DELETE FROM {typeof(T).Name}s WHERE Id = @Id";
        _connection.Execute(sql, new { data.Id });
    }

    public void Remove<T>(IEnumerable<T> list) where T : Model
    {
        var sql = $"DELETE FROM {typeof(T).Name}s WHERE Id IN @Ids";
        _connection.Execute(sql, new { Ids = list.Select(x => x.Id) });
    }

    public async Task RemoveAsync<T>(T data) where T : Model
    {
        var sql = $"DELETE FROM {typeof(T).Name}s WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { data.Id });
    }

    public async Task RemoveAsync<T>(IEnumerable<T> list) where T : Model
    {
        var sql = $"DELETE FROM {typeof(T).Name}s WHERE Id IN @Ids";
        await _connection.ExecuteAsync(sql, new { Ids = list.Select(x => x.Id) });
    }
}