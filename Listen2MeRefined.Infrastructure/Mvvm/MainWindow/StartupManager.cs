using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

public sealed class StartupManager : IDisposable
{
    private readonly IMediator _mediator;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IGlobalHook _globalHook;
    private readonly IFolderScanner _folderScanner;
    private readonly ILogger _logger;
    private readonly DataContext _dataContext;
    private readonly IOutputDevice _outputDevice;

    public StartupManager(
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook,
        IFolderScanner folderScanner,
        IMediator mediator,
        ILogger logger,
        DataContext dataContext, 
        IOutputDevice outputDevice)
    {
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _folderScanner = folderScanner;
        _mediator = mediator;
        _logger = logger;
        _dataContext = dataContext;
        _outputDevice = outputDevice;

        _logger.Debug("[StartupManager] Class initialized");
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.Debug("[StartupManager] Starting StartAsync...");

        await PerformDatabaseMigrationAsync(ct);

        await Task.WhenAll(
            PublishFontFamilyNotificationAsync(ct),
            SelectAudioOutputDeviceAsync(ct),
            StartBackgroundFolderScanAsync(),
            RegisterGlobalHooksAsync());
        
        _logger.Debug("[StartupManager] StartAsync completed.");
    }

    private async Task PerformDatabaseMigrationAsync(CancellationToken ct)
    {
        await _dataContext.Database.MigrateAsync(ct).ConfigureAwait(false);
        _logger.Information("[StartupManager] Database migration completed.");
    }

    private async Task PublishFontFamilyNotificationAsync(CancellationToken ct)
    {
        await _mediator
            .Publish(new FontFamilyChangedNotification(_settingsManager.Settings.FontFamily), ct)
            .ConfigureAwait(false);
        _logger.Debug("[StartupManager] Font family notification published with value {FontFamily}.",
            _settingsManager.Settings.FontFamily);
    }

    private async Task SelectAudioOutputDeviceAsync(CancellationToken ct)
    {
        // Output device selection (off-UI thread if needed)
        var outputDevice = await Task.Run(() =>
        {
            var allDevices = _outputDevice.EnumerateOutputDevices().ToArray();
            return allDevices.FirstOrDefault(x => x.Name == _settingsManager.Settings.AudioOutputDeviceName)
                   ?? allDevices.FirstOrDefault();
        }, ct).ConfigureAwait(false);
        _logger.Information("[StartupManager] Selected audio output device: {DeviceName}.",
            outputDevice?.Name ?? "None");

        if (outputDevice is not null)
        {
            await _mediator.Publish(new AudioOutputDeviceChangedNotification(outputDevice), ct)
                .ConfigureAwait(false);
        }

        _logger.Debug("[StartupManager] Audio output device notification published.");
    }

    private async Task StartBackgroundFolderScanAsync()
    {
        if (_settingsManager.Settings.ScanOnStartup)
        {
            _logger.Information("[StartupManager] Starting folder scan in background...");
            try
            {
                await _folderScanner.ScanAllAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* ignore */ }
            catch (Exception ex)
            {
                _logger.Error(ex, "[StartupManager] Error during background folder scan on startup.");
            }

            _logger.Information("[StartupManager] Background folder scan completed.");
        }
    }

    private async Task RegisterGlobalHooksAsync()
    {
        _logger.Information("[StartupManager] Registering global hooks...");
        // Initialize hooks after everything else is ready
        await _globalHook.RegisterAsync().ConfigureAwait(false);
        _logger.Information("[StartupManager] Global hooks registered.");
    }

    public void Dispose()
    {
        _logger.Verbose("[StartupManager] Unregistering global hooks...");
        _globalHook.Unregister();
    }
}