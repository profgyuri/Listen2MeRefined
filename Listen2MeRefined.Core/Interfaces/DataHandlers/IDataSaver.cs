namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IDataSaver
{
    /// <summary>
    ///     Saves a single <typeparamref name="T"/> entity to the database.
    /// </summary>
    /// <typeparam name="T">The type of the model to save to the database.</typeparam>
    /// <param name="data">Entity to save to the database.</param>
    void Save<T>(T data) where T : class;
}