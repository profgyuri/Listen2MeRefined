namespace Listen2MeRefined.Infrastructure.Data.Dapper;

using global::Dapper.Contrib.Extensions;

public class DapperUpdater : IDataUpdater
{
    private readonly IDbConnection _connection;

    public DapperUpdater(IDbConnection connection)
    {
        _connection = connection;
    }

    public void Update<T>(T data) where T : Model
    {
        _connection.Update(data);
    }

    public void Update<T>(IEnumerable<T> list) where T : Model
    {
        _connection.Update(list);
    }

    public async Task UpdateAsync<T>(T data) where T : Model
    {
        await _connection.UpdateAsync(data);
    }

    public async Task UpdateAsync<T>(IEnumerable<T> list) where T : Model
    {
        await _connection.UpdateAsync(list);
    }
}