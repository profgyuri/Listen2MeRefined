namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public interface IDataUpdater<in T>
    where T : Model
{
    /// <summary>
    ///     Updates a single <typeparamref name="T" /> item in the database asynchronously.
    ///     This method should not create a new entity, if it does not exist already.
    /// </summary>
    /// <typeparam name="T">The type of the model to update.</typeparam>
    /// <param name="data">Entity to update. Usually tracked by the Id column.</param>
    Task UpdateAsync(T data);

    /// <summary>
    ///     Updates multiple <typeparamref name="T" /> items in the database asynchronously.
    ///     This method should not create a new entity, if it does not exist already.
    /// </summary>
    /// <typeparam name="T">The type of the models to update.</typeparam>
    /// <param name="list">List of entities to update. Usually tracked by the Id column.</param>
    Task UpdateAsync(IEnumerable<T> list);
}