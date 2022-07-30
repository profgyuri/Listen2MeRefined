namespace Listen2MeRefined.Infrastructure.Data.Dapper;

public class DapperSaver : IDataSaver
{
    public void Save<T>(T data) where T : class
    {
        throw new NotImplementedException();
    }

    public void Save<T>(IList<T> list) where T : class
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync<T>(T data) where T : class
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync<T>(IList<T> list) where T : class
    {
        throw new NotImplementedException();
    }
}