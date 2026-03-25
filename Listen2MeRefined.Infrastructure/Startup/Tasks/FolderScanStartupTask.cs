using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class FolderScanStartupTask : IStartupTask
{
    private readonly IFolderScanner _folderScanner;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly ILogger _logger;

    public FolderScanStartupTask(
        IFolderScanner folderScanner,
        ISettingsManager<AppSettings> settingsManager,
        ILogger logger)
    {
        _folderScanner = folderScanner;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken ct)
    {
        if (!_settingsManager.Settings.ScanOnStartup)
        {
            return Task.CompletedTask;
        }

        _logger.Information("[FolderScanStartupTask] Starting folder scan in background...");

        _ = RunBackgroundScanAsync(ct);
        return Task.CompletedTask;
    }

    private async Task RunBackgroundScanAsync(CancellationToken ct)
    {
        try
        {
            await _folderScanner.ScanAllAsync(ScanMode.Incremental, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.Information("[FolderScanStartupTask] Background folder scan canceled.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[FolderScanStartupTask] Error during background folder scan on startup.");
        }

        _logger.Information("[FolderScanStartupTask] Background folder scan completed.");
    }
}
