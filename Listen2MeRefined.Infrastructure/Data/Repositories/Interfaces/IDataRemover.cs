namespace Listen2MeRefined.Infrastructure.Data.Repositories.Interfaces;

public interface IDataRemover<T>
    where T : Model
{
    /// <summary>
    ///     Removes a single <typeparamref name="T" /> item from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to remove.</typeparam>
    /// <param name="data">Entity to remove from the database.</param>
    Task RemoveAsync(T data);

    /// <summary>
    ///     Removes multiple <typeparamref name="T" /> items from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the models to remove.</typeparam>
    /// <param name="list">List of entities to remove from the database.</param>
    Task RemoveAsync(IEnumerable<T> list);

    /// <summary>
    ///     Removes all <typeparamref name="T" /> items from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the models to remove.</typeparam>
    Task RemoveAllAsync();
}