using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IAdvancedDataReader<in T1, T2>
    where T2: Model
{
    Task<IEnumerable<T2>> ReadAsync(IEnumerable<T1> criterias, bool matchAll);
}