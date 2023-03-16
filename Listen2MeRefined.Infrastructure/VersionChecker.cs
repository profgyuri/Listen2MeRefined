namespace Listen2MeRefined.Infrastructure;

using Newtonsoft.Json;
using System.Configuration;
using System.Threading.Tasks;
using Version = Listen2MeRefined.Core.Models.Version;

public class VersionChecker : IVersionChecker
{
    private const string apiUrl = "https://api.github.com/repos/profgyuri/Listen2MeRefined/releases";

    public async Task<Version> GetLatestVersionAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "listen2me");

        var json = await client.GetStringAsync(apiUrl);
        var releases = JsonConvert.DeserializeObject<List<Release>>(json)!;
        var latestRelease = releases[0];

        string versionString = latestRelease.Tag_Name;
        return Version.CreateFromString(versionString);
    }

    public async Task<bool> IsLatestAsync()
    {
        var latestVersion = await GetLatestVersionAsync();
        var versionNumber = ConfigurationManager.AppSettings["VersionNumber"] ?? "";
        var currentVersion = Version.CreateFromString(versionNumber);

        return latestVersion <= currentVersion;
    }
}

/// <summary>
/// Simply used to deserialize the GitHub API response.
/// </summary>
class Release
{
    public string Tag_Name { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
}
