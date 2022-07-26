namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

/// <summary>
///     Interface to provide CRUD operations.
/// </summary>
/// <typeparam name="T">The the of entity to make changes for in the database.</typeparam>
public interface IRepository<T>
{
    void Create(T data);
    IList<T> Read();
    void Update(T data);
    void Delete(T data);
}