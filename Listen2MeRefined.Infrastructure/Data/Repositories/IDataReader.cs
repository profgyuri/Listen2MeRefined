namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public interface IDataReader<T>
    where T: Model
{
    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    Task<IEnumerable<T>> ReadAsync();

    /// <summary>
    ///     Returns every entry for the matching <typeparamref name="T" /> type from the database asynchronously.
    /// </summary>
    /// <param name="searchTerm">Return data from every column tath contains this term.</param>
    /// <typeparam name="T">The type of the model to look for.</typeparam>
    /// <returns>A list of <typeparamref name="T" /> models.</returns>
    Task<IEnumerable<T>> ReadAsync(string searchTerm);
}