﻿namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public sealed class EntityFrameworkRemover : IDataRemover
{
    private readonly DataContext _dataContext;

    public EntityFrameworkRemover(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public void Remove<T>(T data)
        where T : Model
    {
        _dataContext.Set<T>().Remove(data);
        _dataContext.SaveChanges();
    }

    public void Remove<T>(IEnumerable<T> list)
        where T : Model
    {
        _dataContext.Set<T>().RemoveRange(list);
        _dataContext.SaveChanges();
    }

    public async Task RemoveAsync<T>(T data)
        where T : Model
    {
        _dataContext.Set<T>().Remove(data);
        await _dataContext.SaveChangesAsync();
    }

    public async Task RemoveAsync<T>(IEnumerable<T> list)
        where T : Model
    {
        _dataContext.Set<T>().RemoveRange(list);
        await _dataContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void RemoveAll<T>()
        where T : Model
    {
        _dataContext.Set<T>().RemoveRange(_dataContext.Set<T>());
        _dataContext.SaveChanges();
    }

    /// <inheritdoc />
    public async Task RemoveAllAsync<T>()
        where T : Model
    {
        _dataContext.Set<T>().RemoveRange(_dataContext.Set<T>());
        await _dataContext.SaveChangesAsync();
    }
}