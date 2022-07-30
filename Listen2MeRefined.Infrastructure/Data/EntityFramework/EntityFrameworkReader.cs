namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;

public class EntityFrameworkReader : IDataReader
{
    private readonly DataContext _dataContext;

    public EntityFrameworkReader(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public IList<T> Read<T>() where T: Model
    {
        return _dataContext.Set<T>().ToList();
    }

    public async Task<IList<T>> ReadAsync<T>() where T : Model
    {
        return await _dataContext.Set<T>().ToListAsync();
    }
}