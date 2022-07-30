namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public class EntityFrameworkSaver : IDataSaver
{
    private readonly DataContext _dataContext;

    public EntityFrameworkSaver(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public void Save<T>(T data) where T : class
    {
        _dataContext.Set<T>().Add(data);
        _dataContext.SaveChanges();
    }

    public void Save<T>(IList<T> list) where T : class
    {
        _dataContext.Set<T>().AddRange(list);
        _dataContext.SaveChanges();
    }

    public async Task SaveAsync<T>(T data) where T : class
    {
        _dataContext.Set<T>().Add(data);
        await _dataContext.SaveChangesAsync();
    }

    public async Task SaveAsync<T>(IList<T> list) where T : class
    {
        _dataContext.Set<T>().AddRange(list);
        await _dataContext.SaveChangesAsync();
    }
}