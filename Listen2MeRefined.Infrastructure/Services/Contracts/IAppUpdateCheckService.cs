using Listen2MeRefined.Infrastructure.Services.Models;

namespace Listen2MeRefined.Infrastructure.Services.Contracts;

/// <summary>
/// Checks whether a newer application version is available.
/// </summary>
public interface IAppUpdateCheckService
{
    /// <summary>
    /// Executes an update check and returns a UI-friendly result.
    /// </summary>
    Task<AppUpdateCheckResult> CheckForUpdatesAsync();
}
