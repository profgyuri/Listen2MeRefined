namespace Listen2MeRefined.Core.Models;

public class MusicFolderModel : Model
{
    public string FullPath { get; set; }

    public MusicFolderModel(string path)
    {
        FullPath = path;
    }
}