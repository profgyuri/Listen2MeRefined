namespace Listen2MeRefined.Infrastructure.Services.Contracts;

/// <summary>
/// Synchronizes global hook registration based on current settings flags.
/// </summary>
public interface IGlobalHookSettingsSyncService
{
    /// <summary>
    /// Registers or unregisters global hooks based on the supplied settings.
    /// </summary>
    Task SyncAsync(bool enableGlobalMediaKeys, bool enableCornerNowPlayingPopup);
}
