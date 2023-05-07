using System.ComponentModel.DataAnnotations;
using Listen2MeRefined.Infrastructure.Storage;

namespace Listen2MeRefined.Infrastructure.Data;

public sealed class AppSettings : Settings
{
    [Key] public int Id { get; set; }

    public string FontFamily { get; set; } = "";
    public List<MusicFolderModel> MusicFolders { get; set; } = new();
    public bool ScanOnStartup { get; set; } = true;
}