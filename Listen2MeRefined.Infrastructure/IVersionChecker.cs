namespace Listen2MeRefined.Infrastructure;

using Version = Core.Models.Version;

public interface IVersionChecker
{
    /// <summary>
    /// Get the latest version from the GitHub API.
    /// </summary>
    Task<Version> GetLatestVersionAsync();

    /// <summary>
    /// Check if the current version is the latest.
    /// </summary>
    Task<bool> IsLatestAsync();

    /// <summary>
    /// Open the GitHub release page in the default browser.
    /// </summary>
    void OpenUpdateLink();
}
