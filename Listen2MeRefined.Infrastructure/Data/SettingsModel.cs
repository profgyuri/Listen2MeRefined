using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Data;

public sealed class SettingsModel : Settings
{
    public string FontFamily { get; set; } = "";
    public IEnumerable<string> MusicFolders { get; set; } = new List<string>();
}