namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IRepository<T>
{
    void Create(T data);
    IList<T> Read();
    void Update(T data);
    void Delete(T data);
}