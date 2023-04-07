namespace Listen2MeRefined.Infrastructure.Data.Repositories.Interfaces;

public interface IDataSaver<in T>
    where T: Model
{
    /// <summary>
    ///     Saves a single <typeparamref name="T" /> entity to the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to save to the database.</typeparam>
    /// <param name="data">Entity to save to the database.</param>
    Task SaveAsync(T data);

    /// <summary>
    ///     Saves multiple <typeparamref name="T" /> entities to the database asynvhronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to save to the database.</typeparam>
    /// <param name="list">List of entities to save to the database.</param>
    Task SaveAsync(IEnumerable<T> list);
}