namespace Listen2MeRefined.Infrastructure.SystemOperations;
using System.Collections.Generic;

public class FolderBrowser : IFolderBrowser
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
