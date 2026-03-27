using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Core.Repositories;

public interface IAdvancedDataReader<in T1, T2>
    where T2: ModelBase
{
    /// <summary>
    ///     Reads models that match provided advanced filters.
    ///     Empty filter collections return an empty result set.
    /// </summary>
    Task<IEnumerable<T2>> ReadAsync(IEnumerable<T1> criterias, bool matchAll);
}