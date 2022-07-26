namespace Listen2MeRefined.Core.Interfaces.DataHandlers;
public interface IDataRemover
{
    /// <summary>
    ///     Removes a single <typeparamref name="T"/> item from the database.
    /// </summary>
    /// <typeparam name="T">The type of the model to remove.</typeparam>
    /// <param name="data">Entity to remove from the database.</param>
    void Remove<T>(T data) where T : class;
}