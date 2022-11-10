namespace Listen2MeRefined.Core.Models;

public sealed class MusicFolderModel : Model
{
    public string FullPath { get; set; }

    public MusicFolderModel()
    {
        
    }
    
    public MusicFolderModel(string path)
    {
        FullPath = path;
    }
}