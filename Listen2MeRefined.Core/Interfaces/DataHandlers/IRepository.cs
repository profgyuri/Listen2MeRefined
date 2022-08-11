namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

/// <summary>
///     Interface to provide CRUD operations.
/// </summary>
/// <typeparam name="T">The the of entity to make changes for in the database.</typeparam>
public interface IRepository<T>
{
    void Create(T data);
    Task CreateAsync(T data);
    void Create(IEnumerable<T> data);
    Task CreateAsync(IEnumerable<T> data);
    IEnumerable<T> Read();
    Task<IEnumerable<T>> ReadAsync();
    void Update(T data);
    Task UpdateAsync(T data);
    void Update(IEnumerable<T> data);
    Task UpdateAsync(IEnumerable<T> data);
    void Delete(T data);
    Task DeleteAsync(T data);
    void Delete(IEnumerable<T> data);
    Task DeleteAsync(IEnumerable<T> data);
}