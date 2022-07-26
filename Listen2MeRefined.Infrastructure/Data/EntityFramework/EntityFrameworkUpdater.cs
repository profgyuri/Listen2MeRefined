namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public class EntityFrameworkUpdater : IDataUpdater
{
    private readonly DataContext _dataContext;

    public EntityFrameworkUpdater(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public void Update<T>(T data) where T : class
    {
        _dataContext.Set<T>().Update(data);
        _dataContext.SaveChanges();
    }
}