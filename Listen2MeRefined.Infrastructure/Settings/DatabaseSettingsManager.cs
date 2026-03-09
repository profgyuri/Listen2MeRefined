using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Settings;

public sealed class DatabaseSettingsManager<T> : ISettingsManager<T>
    where T : Application.Settings.Settings, new()
{
    private readonly IDbContextFactory<DataContext> _dataContextFactory;
    private readonly ILogger _logger;
    private readonly object _settingsLock = new();
    private T? _settings;

    public DatabaseSettingsManager(IDbContextFactory<DataContext> dataContextFactory, ILogger logger)
    {
        _dataContextFactory = dataContextFactory;
        _logger = logger;
    }

    private T LoadSettings()
    {
        using var dataContext = _dataContextFactory.CreateDbContext();
        return dataContext.Settings
            .AsNoTracking()
            .Include(x => x.MusicFolders)
            .FirstOrDefault() as T ?? new T();
    }

    public T Settings
    {
        get
        {
            lock (_settingsLock)
            {
                return _settings ??= LoadSettings();
            }
        }
    }

    public void SaveSettings(Action<T>? settings = null)
    {
        lock (_settingsLock)
        {
            using var dataContext = _dataContextFactory.CreateDbContext();
            var persistedSettings = dataContext.Settings
                .Include(x => x.MusicFolders)
                .FirstOrDefault() as T ?? new T();

            settings?.Invoke(persistedSettings);

            var appSettings = (persistedSettings as AppSettings)!;
            dataContext.Settings.Update(appSettings);
            dataContext.MusicFolders.UpdateRange(appSettings.MusicFolders);
            var saved = false;
            while (!saved)
            {
                try
                {
                    dataContext.SaveChanges();
                    _settings = persistedSettings;
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
                            _logger.Fatal("[DatabaseSettingsManager] Don't know how to handle concurrency conflicts for {name}", entry.Metadata.Name);
                            throw new NotSupportedException("Concurrency conflicts are not supported.");
                        }
                    }
                }
            }
        }
    }
}
