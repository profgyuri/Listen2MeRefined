namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public class EntityFrameworkRemover : IDataRemover
{
    private readonly DataContext _dataContext;

    public EntityFrameworkRemover(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public void Remove<T>(T data) where T : class
    {
        _dataContext.Set<T>().Remove(data);
    }
}