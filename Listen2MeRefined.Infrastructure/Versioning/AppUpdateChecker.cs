using Listen2MeRefined.Infrastructure.Services.Models;

namespace Listen2MeRefined.Infrastructure.Versioning;

public sealed class AppUpdateChecker : IAppUpdateChecker
{
    private readonly IVersionChecker _versionChecker;
    private readonly ILogger _logger;

    public AppUpdateChecker(IVersionChecker versionChecker, ILogger logger)
    {
        _versionChecker = versionChecker;
        _logger = logger;
    }

    public async Task<AppUpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            if (await _versionChecker.IsLatestAsync())
            {
                return new AppUpdateCheckResult(
                    IsUpdateAvailable: false,
                    Message: "You are using the latest version.",
                    CanOpenUpdateLink: false);
            }

            return new AppUpdateCheckResult(
                IsUpdateAvailable: true,
                Message: "A newer version is available.",
                CanOpenUpdateLink: true);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[AppUpdateCheckService] Update check failed");
            return new AppUpdateCheckResult(
                IsUpdateAvailable: false,
                Message: "Could not check for updates.",
                CanOpenUpdateLink: false);
        }
    }
}
