namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

using Listen2MeRefined.Core.Models;

public interface IDataReader
{
    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T"/> type from the database.
    /// </summary>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    IList<T> Read<T>() where T: Model;

    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T"/> type from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    Task<IList<T>> ReadAsync<T>() where T : Model;
}