using System.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Data;

public class DatabaseSettingsManager<T> : ISettingsManager<T>
    where T: Settings, new()
{
    private DataContext _dataContext;
    
    private T? _settings;
    
    public DatabaseSettingsManager(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    #region Implementation of ISettingsManager<out T>
    /// <inheritdoc />
    public T? Settings => _settings ??= LoadSettings();

    /// <inheritdoc />
    public void SaveSettings(Action<T>? settings = null)
    {
        var oldSettings = LoadSettings();
        
        settings?.Invoke(oldSettings!);
        
        //_dataContext.Settings.Update((oldSettings as AppSettings)!);
    }
    #endregion
    
    private T LoadSettings()
    {
        //return _dataContext.Settings.FirstOrDefault() as T ?? new T();
        return new T();
    }
}