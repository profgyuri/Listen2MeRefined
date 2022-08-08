namespace Listen2MeRefined.Core.Models;

public class SettingsModel
{
    public string FontFamily { get; set; } = "";
    public IEnumerable<string> MusicFolders { get; set; } = new List<string>();
}