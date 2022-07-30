namespace Listen2MeRefined.Core.Interfaces.DataHandlers;
public interface IDataRemover
{
    /// <summary>
    ///     Removes a single <typeparamref name="T"/> item from the database.
    /// </summary>
    /// <typeparam name="T">The type of the model to remove.</typeparam>
    /// <param name="data">Entity to remove from the database.</param>
    void Remove<T>(T data) where T : class;

    /// <summary>
    ///     Removes a single <typeparamref name="T"/> item from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to remove.</typeparam>
    /// <param name="data">Entity to remove from the database.</param>
    Task RemoveAsync<T>(T data) where T : class;

    /// <summary>
    ///     Removes multiple <typeparamref name="T"/> items from the database.
    /// </summary>
    /// <typeparam name="T">The type of the models to remove.</typeparam>
    /// <param name="list">List of entities to remove from the database.</param>
    void Remove<T>(IList<T> list) where T : class;

    /// <summary>
    ///     Removes multiple <typeparamref name="T"/> items from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the models to remove.</typeparam>
    /// <param name="list">List of entities to remove from the database.</param>
    Task RemoveAsync<T>(IList<T> list) where T : class;
}