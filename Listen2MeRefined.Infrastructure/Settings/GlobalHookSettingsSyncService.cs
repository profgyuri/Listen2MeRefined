namespace Listen2MeRefined.Infrastructure.Settings;

public sealed class GlobalHookSettingsSyncService : IGlobalHookSettingsSyncService
{
    private readonly IGlobalHook _globalHook;
    private readonly ILogger _logger;

    public GlobalHookSettingsSyncService(IGlobalHook globalHook, ILogger logger)
    {
        _globalHook = globalHook;
        _logger = logger;
    }

    public async Task SyncAsync(bool enableGlobalMediaKeys, bool enableCornerNowPlayingPopup)
    {
        try
        {
            if (enableGlobalMediaKeys || enableCornerNowPlayingPopup)
            {
                await _globalHook.RegisterAsync();
                return;
            }

            _globalHook.Unregister();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[GlobalHookSettingsSyncService] Failed to synchronize global hook state");
        }
    }
}
