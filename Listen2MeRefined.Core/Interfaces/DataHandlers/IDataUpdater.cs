namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IDataUpdater
{
    /// <summary>
    ///     Updates a single <typeparamref name="T"/> item in the database. 
    ///     This method should not create a new entity, if it does not exist already.
    /// </summary>
    /// <typeparam name="T">The type of the model to update.</typeparam>
    /// <param name="data">Entity to update. Usually tracked by the Id column.</param>
    void Update<T>(T data) where T : class;
}