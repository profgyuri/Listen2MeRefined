namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IDataUpdater
{
    void Update<T>(T data) where T : class;
}