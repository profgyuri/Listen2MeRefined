using System.Text;

namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;

public class EntityFrameworkReader : IDataReader
{
    private readonly DataContext _dataContext;

    public EntityFrameworkReader(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public IEnumerable<T> Read<T>() where T: Model
    {
        return _dataContext.Set<T>().ToList();
    }

    public async Task<IEnumerable<T>> ReadAsync<T>() where T : Model
    {
        return await _dataContext.Set<T>().ToListAsync();
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(string searchTerm) where T : Model
    {
        var properties = typeof(T).GetProperties();
        
        return _dataContext.Set<T>().Where(x => properties.Any(y => y.GetValue(x).ToString().Contains(searchTerm)));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(string searchTerm) where T : Model
    {
        var properties = typeof(T).GetProperties();
        
        return await _dataContext.Set<T>().Where(x => properties.Any(y => y.GetValue(x).ToString().Contains(searchTerm))).ToListAsync();
    }

    /// <inheritdoc />
    public IEnumerable<T> Read<T>(T model, bool exact) where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();
        
        var whereClause = new StringBuilder();
        foreach (var property in properties)
        {
            if (exact)
            {
                whereClause.Append($"{property} = @{property} AND ");
            }
            else
            {
                whereClause.Append($"{property} LIKE @%{property}% AND ");
            }
        }
        
        whereClause.Remove(whereClause.Length - 5, 5);
        
        var sql = $"SELECT * FROM {typeof(T).Name} WHERE {whereClause}";
        return _dataContext.Set<T>().FromSqlRaw(sql, model);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ReadAsync<T>(T model, bool exact) where T : Model
    {
        var properties = typeof(T)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();
        
        var whereClause = new StringBuilder();
        foreach (var property in properties)
        {
            if (exact)
            {
                whereClause.Append($"{property} = @{property} AND ");
            }
            else
            {
                whereClause.Append($"{property} LIKE @%{property}% AND ");
            }
        }
        
        whereClause.Remove(whereClause.Length - 5, 5);
        
        var sql = $"SELECT * FROM {typeof(T).Name} WHERE {whereClause}";
        return await _dataContext.Set<T>().FromSqlRaw(sql, model).ToListAsync();
    }
}