namespace Listen2MeRefined.Infrastructure.Versioning;

/// <summary>
/// Checks whether a newer application version is available.
/// </summary>
public interface IAppUpdateChecker
{
    /// <summary>
    /// Executes an update check and returns a UI-friendly result.
    /// </summary>
    Task<AppUpdateCheckResult> CheckForUpdatesAsync();
}
