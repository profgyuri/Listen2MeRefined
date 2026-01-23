namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

public class StartupManager : IDisposable
{
    private readonly IMediator _mediator;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IGlobalHook _globalHook;
    private readonly IFolderScanner _folderScanner;
    private readonly DataContext _dataContext;
    private bool disposedValue;

    public StartupManager(
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook,
        IFolderScanner folderScanner,
        DataContext dataContext,
        IMediator mediator)
    {
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _folderScanner = folderScanner;
        _dataContext = dataContext;
        _mediator = mediator;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        // Initialize hooks last and don't block startup
        await _dataContext.Database.MigrateAsync(cancellationToken: ct);
        await _mediator.Publish(new FontFamilyChangedNotification(_settingsManager.Settings.FontFamily), ct);

        if (_settingsManager.Settings.ScanOnStartup)
        {
            await _folderScanner.ScanAllAsync(ct);
        }

        var allDevices = AudioDevices.GetOutputDevices();
        var outputDevice =
            allDevices.FirstOrDefault(x => x.Name == _settingsManager.Settings.AudioOutputDeviceName) ??
            allDevices.FirstOrDefault()!;
        await _mediator.Publish(new AudioOutputDeviceChangedNotification(outputDevice), ct);

        // Initialize hooks after everything else is ready
        if (!ct.IsCancellationRequested)
        {
            await _globalHook.RegisterAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _globalHook.Unregister();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}