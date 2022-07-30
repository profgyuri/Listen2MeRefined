namespace Listen2MeRefined.Infrastructure.Data.Dapper;

public class DapperUpdater : IDataUpdater
{
    public void Update<T>(T data) where T : class
    {
        throw new NotImplementedException();
    }

    public void Update<T>(IList<T> list) where T : class
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync<T>(T data) where T : class
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync<T>(IList<T> list) where T : class
    {
        throw new NotImplementedException();
    }
}