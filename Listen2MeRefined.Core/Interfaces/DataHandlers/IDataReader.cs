namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IDataReader
{
    IList<T> Read<T>();
}