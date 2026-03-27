using Listen2MeRefined.Application.Folders;

namespace Listen2MeRefined.Infrastructure.FolderBrowser;

public sealed class PinnedFoldersService : IPinnedFoldersService
{
    private readonly IFolderBrowser _folderBrowser;

    public PinnedFoldersService(IFolderBrowser folderBrowser)
    {
        _folderBrowser = folderBrowser;
    }

    public IReadOnlyList<string> Normalize(IEnumerable<string> folders)
    {
        return folders
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> NormalizeExisting(IEnumerable<string> folders)
    {
        return Normalize(folders)
            .Where(_folderBrowser.DirectoryExists)
            .ToList();
    }

    public IReadOnlyList<string> TogglePinnedFolder(IEnumerable<string> currentFolders, string folderPath)
    {
        var pinned = NormalizeExisting(currentFolders).ToList();
        if (string.IsNullOrWhiteSpace(folderPath) || !_folderBrowser.DirectoryExists(folderPath))
        {
            return pinned;
        }

        var existingIndex = pinned.FindIndex(x => x.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            pinned.RemoveAt(existingIndex);
            return pinned;
        }

        pinned.Insert(0, folderPath);
        return pinned;
    }
}
