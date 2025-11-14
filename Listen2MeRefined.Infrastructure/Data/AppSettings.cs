namespace Listen2MeRefined.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using Listen2MeRefined.Infrastructure.Storage;

public sealed class AppSettings : Settings
{
    [Key] public int Id { get; set; }

    public string FontFamily { get; set; } = "";
    public string NewSongWindowPosition { get; set; } = "";
    public string AudioOutputDeviceName { get; set; } = "";
    public List<MusicFolderModel> MusicFolders { get; set; } = new();
    public bool ScanOnStartup { get; set; } = true;
}