using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Data;

public sealed class AppSettings : Settings
{
    internal string FontFamily { get; set; } = "";
    internal IEnumerable<string> MusicFolders { get; set; } = new List<string>();
}