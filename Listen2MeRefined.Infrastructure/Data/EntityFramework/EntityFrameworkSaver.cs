namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public class EntityFrameworkSaver : IDataSaver
{
    private readonly DataContext _dataContext;

    public EntityFrameworkSaver(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public void Save<T>(T data) where T : Model
    {
        _dataContext.AddIfDoesNotExist(data);
        _dataContext.SaveChanges();
    }

    public void Save<T>(IEnumerable<T> list) where T : Model
    {
        _dataContext.AddIfDoesNotExist(list);
        _dataContext.SaveChanges();
    }

    public async Task SaveAsync<T>(T data) where T : Model
    {
        _dataContext.AddIfDoesNotExist(data);
        await _dataContext.SaveChangesAsync();
    }

    public async Task SaveAsync<T>(IEnumerable<T> list) where T : Model
    {
        await _dataContext.AddIfDoesNotExistAsync(list);
        await _dataContext.SaveChangesAsync();
    }
}