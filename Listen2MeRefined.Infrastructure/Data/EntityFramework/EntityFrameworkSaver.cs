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
}