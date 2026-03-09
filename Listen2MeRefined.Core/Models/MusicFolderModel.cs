namespace Listen2MeRefined.Core.Models;

public sealed class MusicFolderModel : ModelBase
{
    public string FullPath { get; init; }
    public bool IncludeSubdirectories { get; set; }
    
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