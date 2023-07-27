namespace Listen2MeRefined.Infrastructure;

using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using Version = Listen2MeRefined.Core.Models.Version;

public class VersionChecker : IVersionChecker
{
    private readonly Version versionNumber = Version.FromVersionNumbers(0, 7, 0);
    private const string apiUrl = "https://api.github.com/repos/profgyuri/Listen2MeRefined/releases";
    private Release? _latest;

    public async Task<Version> GetLatestVersionAsync()
    {
        if (_latest is not null)
        {
            return _latest.Version;
        }

        if (await IsInternetAccessible() is false)
        {
            return versionNumber;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "listen2me");

        var json = await client.GetStringAsync(apiUrl);
        var releases = JsonConvert.DeserializeObject<List<Release>>(json)!;
        _latest = releases[0];

        return _latest.Version;
    }

    public async Task<bool> IsLatestAsync()
    {
        if (await IsInternetAccessible() is false)
        {
            return true;
        }

        var latestVersion = await GetLatestVersionAsync();

        return latestVersion <= versionNumber;
    }

    public void OpenUpdateLink()
    {
        var url = _latest?.Assets[0].Browser_Download_Url ?? "https://github.com/profgyuri/Listen2MeRefined/releases";
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    private async Task<bool> IsInternetAccessible()
    {
        try
        {
            using var client = new HttpClient();
            using var stream = await client.GetAsync("https://www.google.com");
            return true;
        }
        catch
        {
            return false;
        }
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
    public Asset[] Assets { get; set; }
    public Version Version => Version.FromString(Tag_Name);
}

class Asset
{
    public string Browser_Download_Url { get; set; }
}