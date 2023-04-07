namespace Listen2MeRefined.Infrastructure.Data.Repositories.Interfaces;

public interface IAdvancedDataReader<in T1, T2>
    where T2 : Model
{
    Task<IEnumerable<T2>> ReadAsync(IEnumerable<T1> criterias, bool matchAll);
}