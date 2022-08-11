namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

using Listen2MeRefined.Core.Models;

public interface IDataSaver
{
    /// <summary>
    ///     Saves a single <typeparamref name="T"/> entity to the database.
    /// </summary>
    /// <typeparam name="T">The type of the model to save to the database.</typeparam>
    /// <param name="data">Entity to save to the database.</param>
    void Save<T>(T data) where T : Model;

    /// <summary>
    ///     Saves a single <typeparamref name="T"/> entity to the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to save to the database.</typeparam>
    /// <param name="data">Entity to save to the database.</param>
    Task SaveAsync<T>(T data) where T : Model;

    /// <summary>
    ///     Saves multiple <typeparamref name="T"/> entities to the database.
    /// </summary>
    /// <typeparam name="T">The type of the model to save to the database.</typeparam>
    /// <param name="list">List of entities to save to the database.</param>
    void Save<T>(IEnumerable<T> list) where T : Model;

    /// <summary>
    ///     Saves multiple <typeparamref name="T"/> entities to the database asynvhronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to save to the database.</typeparam>
    /// <param name="list">List of entities to save to the database.</param>
    Task SaveAsync<T>(IEnumerable<T> list) where T : Model;
}