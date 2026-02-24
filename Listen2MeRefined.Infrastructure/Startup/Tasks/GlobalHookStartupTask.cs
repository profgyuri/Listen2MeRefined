namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class GlobalHookStartupTask : IStartupTask
{
    private readonly IGlobalHook _globalHook;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly ILogger _logger;

    public GlobalHookStartupTask(
        IGlobalHook globalHook,
        ISettingsManager<AppSettings> settingsManager,
        ILogger logger)
    {
        _globalHook = globalHook;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var settings = _settingsManager.Settings;
        if (!settings.EnableGlobalMediaKeys && !settings.EnableCornerNowPlayingPopup)
        {
            _logger.Information("[GlobalHookStartupTask] Global hooks are disabled by settings.");
            return;
        }

        _logger.Information("[GlobalHookStartupTask] Registering global hooks...");
        await _globalHook.RegisterAsync().ConfigureAwait(false);
        _logger.Information("[GlobalHookStartupTask] Global hooks registered.");
    }
}
