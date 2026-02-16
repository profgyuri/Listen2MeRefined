using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;

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

    public async Task RunAsync(CancellationToken ct)
    {
        if (!_settingsManager.Settings.ScanOnStartup)
        {
            return;
        }

        _logger.Information("[FolderScanStartupTask] Starting folder scan in background...");

        try
        {
            await _folderScanner.ScanAllAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[FolderScanStartupTask] Error during background folder scan on startup.");
        }

        _logger.Information("[FolderScanStartupTask] Background folder scan completed.");
    }
}
