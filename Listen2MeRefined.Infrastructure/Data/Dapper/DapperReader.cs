namespace Listen2MeRefined.Infrastructure.Data.Dapper;

public class DapperReader : IDataReader
{
    public IList<T> Read<T>() where T : class
    {
        throw new NotImplementedException();
    }

    public Task<IList<T>> ReadAsync<T>() where T : class
    {
        throw new NotImplementedException();
    }
}