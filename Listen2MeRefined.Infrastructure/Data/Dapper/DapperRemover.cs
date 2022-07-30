namespace Listen2MeRefined.Infrastructure.Data.Dapper;

public class DapperRemover : IDataRemover
{
    public void Remove<T>(T data) where T : class
    {
        throw new NotImplementedException();
    }

    public void Remove<T>(IList<T> list) where T : class
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync<T>(T data) where T : class
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync<T>(IList<T> list) where T : class
    {
        throw new NotImplementedException();
    }
}