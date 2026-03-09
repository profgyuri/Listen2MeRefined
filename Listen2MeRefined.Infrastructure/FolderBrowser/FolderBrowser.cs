using Listen2MeRefined.Application.Folders;

namespace Listen2MeRefined.Infrastructure.FolderBrowser;

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

    public IEnumerable<string> GetSubFoldersSafe(string path)
    {
        try
        {
            return GetSubFolders(path);
        }
        catch (UnauthorizedAccessException)
        {
            return Enumerable.Empty<string>();
        }
        catch (DirectoryNotFoundException)
        {
            return Enumerable.Empty<string>();
        }
        catch (IOException)
        {
            return Enumerable.Empty<string>();
        }
    }

    public bool DirectoryExists(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
    }

    public string? GetParent(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Directory.GetParent(path)?.FullName;
    }
}
