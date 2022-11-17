using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IDataReader
{
    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database.
    /// </summary>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    IEnumerable<T> Read<T>()
        where T : Model;

    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    Task<IEnumerable<T>> ReadAsync<T>()
        where T : Model;

    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database.
    /// </summary>
    /// <param name="searchTerm">Return data from every column tath contains this term.</param>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    /// <returns>A list of <typeparamref name="T" /> models.</returns>
    IEnumerable<T> Read<T>(string searchTerm)
        where T : Model;

    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database asynchronously.
    /// </summary>
    /// <param name="searchTerm">Return data from every column tath contains this term.</param>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    /// <returns>A list of <typeparamref name="T" /> models.</returns>
    Task<IEnumerable<T>> ReadAsync<T>(string searchTerm)
        where T : Model;

    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database.
    /// </summary>
    /// <param name="model">The model to look for by specifying properties. Empty or default properties should be ignored.</param>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    /// <returns>A list of <typeparamref name="T" /> models.</returns>
    IEnumerable<T> Read<T>(
        T model,
        bool exact)
        where T : Model;

    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database asynchronously.
    /// </summary>
    /// <param name="model">The model to look for by specifying properties. Empty or default properties should be ignored.</param>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    /// <returns>A list of <typeparamref name="T" /> models.</returns>
    Task<IEnumerable<T>> ReadAsync<T>(
        T model,
        bool exact)
        where T : Model;
}