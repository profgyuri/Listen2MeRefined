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

    private readonly CancellationTokenSource _cts = new();

    public StartupManager(
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook,
        IFolderScanner folderScanner,
        IMediator mediator,
        ILogger logger,
        DataContext dataContext)
    {
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _folderScanner = folderScanner;
        _mediator = mediator;
        _logger = logger;
        _dataContext = dataContext;

        _logger.Verbose("[StartupManager] Class initialized");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("[StartupManager] Starting StartAsync...");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var ct = linked.Token;

        // Critical startup tasks
        await _dataContext.Database.MigrateAsync(ct).ConfigureAwait(false);
        _logger.Information("[StartupManager] Database migration completed.");

        await _mediator.Publish(
            new FontFamilyChangedNotification(_settingsManager.Settings.FontFamily), ct
        ).ConfigureAwait(false);
        _logger.Information("[StartupManager] Font family notification published with value {FontFamily}.",
            _settingsManager.Settings.FontFamily);

        // Output device selection (off-UI thread if needed)
        var outputDevice = await Task.Run(() =>
        {
            var allDevices = AudioDevices.GetOutputDevices();
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

        _logger.Information("[StartupManager] Audio output device notification published.");

        // Background scan (don’t block startup)
        if (_settingsManager.Settings.ScanOnStartup)
        {
            _logger.Information("[StartupManager] Starting folder scan in background...");
            _ = Task.Run(async () =>
            {
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
            }, ct);
        }

        _logger.Information("[StartupManager] Registering global hooks...");
        // Initialize hooks after everything else is ready
        await _globalHook.RegisterAsync().ConfigureAwait(false);
        _logger.Information("[StartupManager] Global hooks registered.");
        _logger.Information("[StartupManager] StartAsync completed.");
    }

    public void Dispose()
    {
        _logger.Verbose("[StartupManager] Disposing...");
        _cts.Cancel();
        _cts.Dispose();
        _logger.Verbose("[StartupManager] Unregistering global hooks...");
        _globalHook.Unregister();
        _logger.Verbose("[StartupManager] Disposed.");
    }
}