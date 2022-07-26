namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IDataSaver
{
    void Save<T>(T data) where T : class;
}