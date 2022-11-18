namespace Listen2MeRefined.Infrastructure.SystemOperations;

public sealed class FolderBrowser : IFolderBrowser
{
    public IEnumerable<string> GetDrives()
    {
        return Directory.GetLogicalDrives();
    }

    public IEnumerable<string> GetSubFolders(string path)
    {
        return Directory.GetDirectories(path)
            .Select(x => new DirectoryInfo(x).Name);
    }
}