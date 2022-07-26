namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

using System.Collections.Generic;

public class EntityFrameworkReader : IDataReader
{
    private readonly DataContext _dataContext;

    public EntityFrameworkReader(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public IList<T> Read<T>() where T: class
    {
        return _dataContext.Set<T>().ToList();
    }
}