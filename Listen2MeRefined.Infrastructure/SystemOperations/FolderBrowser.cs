namespace Listen2MeRefined.Infrastructure.SystemOperations;
using System.Collections.Generic;

public class FolderBrowser : IFolderBrowser
{
    public IEnumerable<string> GetDrives()
    {
        return new List<string>();
    }

    public IEnumerable<string> GetSubFolders(string path)
    {
        return new List<string>();
    }
}
