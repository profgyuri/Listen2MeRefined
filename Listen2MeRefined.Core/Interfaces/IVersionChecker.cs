namespace Listen2MeRefined.Core.Interfaces;

using Version = Listen2MeRefined.Core.Models.Version;

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
}
