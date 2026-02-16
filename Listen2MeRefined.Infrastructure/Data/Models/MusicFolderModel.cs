namespace Listen2MeRefined.Infrastructure.Data.Models;

public sealed class MusicFolderModel : Model
{
    public string FullPath { get; init; }

    public MusicFolderModel()
    {
        FullPath = string.Empty;
    }
    
    public MusicFolderModel(string path)
    {
        FullPath = path;
    }

    public override string ToString()
    {
        return FullPath;
    }
}