namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public interface IAdvancedDataReader<in T1, T2>
    where T2: Model
{
    /// <summary>
    ///     Reads models that match provided advanced filters.
    ///     Empty filter collections return an empty result set.
    /// </summary>
    Task<IEnumerable<T2>> ReadAsync(IEnumerable<T1> criterias, bool matchAll);
}