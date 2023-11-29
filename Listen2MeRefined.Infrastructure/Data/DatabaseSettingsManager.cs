namespace Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

public sealed class DatabaseSettingsManager<T> : ISettingsManager<T>
    where T : Settings, new()
{
    private readonly DataContext _dataContext;
    private readonly ILogger _logger;

    private T? _settings;

    public DatabaseSettingsManager(DataContext dataContext, ILogger logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    private T LoadSettings()
    {
        return _dataContext.Settings
            .Include(x => x.MusicFolders)
            .FirstOrDefault() as T ?? new T();
    }

    #region Implementation of ISettingsManager<out T>
    /// <inheritdoc />
    public T Settings => _settings ??= LoadSettings();

    /// <inheritdoc />
    public void SaveSettings(Action<T>? settings = null)
    {
        var oldSettings = LoadSettings();

        settings?.Invoke(oldSettings!);

        _dataContext.Settings.Update((oldSettings as AppSettings)!);
        _dataContext.MusicFolders.UpdateRange((oldSettings as AppSettings)!.MusicFolders);
        var saved = false;
        while (!saved)
        {
            try
            {
                _dataContext.SaveChanges();
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is MusicFolderModel)
                    {
                        entry.OriginalValues.SetValues(entry.CurrentValues);
                    }
                    else
                    {
                        var message = "Don't know how to handle concurrency conflicts for " + entry.Metadata.Name;
                        _logger.Fatal(message);
                        throw new NotSupportedException(message);
                    }
                }
            }
        }
    }
    #endregion
}