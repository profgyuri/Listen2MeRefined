namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public class EntityFrameworkUpdater : IDataUpdater
{
    private readonly DataContext _dataContext;

    public EntityFrameworkUpdater(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public void Update<T>(T data) where T : Model
    {
        _dataContext.Set<T>().Update(data);
        _dataContext.SaveChanges();
    }

    public void Update<T>(IList<T> list) where T : Model
    {
        _dataContext.Set<T>().UpdateRange(list);
        _dataContext.SaveChanges();
    }

    public async Task UpdateAsync<T>(T data) where T : Model
    {
        _dataContext.Set<T>().Update(data);
        await _dataContext.SaveChangesAsync();
    }

    public async Task UpdateAsync<T>(IList<T> list) where T : Model
    {
        _dataContext.Set<T>().UpdateRange(list);
        await _dataContext.SaveChangesAsync();
    }
}