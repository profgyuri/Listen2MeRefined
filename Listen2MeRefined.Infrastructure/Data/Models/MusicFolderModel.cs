namespace Listen2MeRefined.Infrastructure.Data.Models;

public sealed class MusicFolderModel : Model
{
    public string FullPath { get; init; }
    public bool IncludeSubdirectories { get; set; }

    public MusicFolderModel()
    {
        FullPath = string.Empty;
        IncludeSubdirectories = false;
    }
    
    public MusicFolderModel(string path, bool includeSubdirectories = false)
    {
        FullPath = path;
        IncludeSubdirectories = includeSubdirectories;
    }

    public override string ToString()
    {
        return FullPath;
    }
}
