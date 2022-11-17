using Dapper.Contrib.Extensions;

namespace Listen2MeRefined.Infrastructure.Data.Dapper;

public class DapperSaver : IDataSaver
{
    private readonly IDbConnection _connection;

    public DapperSaver(IDbConnection connection)
    {
        _connection = connection;
    }

    public void Save<T>(T data)
        where T : Model
    {
        _connection.Insert(data);
    }

    public void Save<T>(IEnumerable<T> list)
        where T : Model
    {
        _connection.Insert(list);
    }

    public async Task SaveAsync<T>(T data)
        where T : Model
    {
        await _connection.InsertAsync(data);
    }

    public async Task SaveAsync<T>(IEnumerable<T> list)
        where T : Model
    {
        await _connection.InsertAsync(list);
    }
}