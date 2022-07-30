namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

/// <summary>
///     Interface to provide CRUD operations.
/// </summary>
/// <typeparam name="T">The the of entity to make changes for in the database.</typeparam>
public interface IRepository<T>
{
    void Create(T data);
    Task CreateAsync(T data);
    void Create(IList<T> data);
    Task CreateAsync(IList<T> data);
    IList<T> Read();
    Task<IList<T>> ReadAsync();
    void Update(T data);
    Task UpdateAsync(T data);
    void Update(IList<T> data);
    Task UpdateAsync(IList<T> data);
    void Delete(T data);
    Task DeleteAsync(T data);
    void Delete(IList<T> data);
    Task DeleteAsync(IList<T> data);
}