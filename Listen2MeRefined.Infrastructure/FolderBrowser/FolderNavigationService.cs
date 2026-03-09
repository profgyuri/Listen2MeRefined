using Listen2MeRefined.Application;
using Listen2MeRefined.Application.Folders;

namespace Listen2MeRefined.Infrastructure.FolderBrowser;

public sealed class FolderNavigationService : IFolderNavigationService
{
    private readonly IFolderBrowser _folderBrowser;

    public FolderNavigationService(IFolderBrowser folderBrowser)
    {
        _folderBrowser = folderBrowser;
    }

    public IReadOnlyList<string> GetDrives()
    {
        return _folderBrowser
            .GetDrives()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string ResolveInitialPath(bool startAtLastLocation, string lastBrowsedFolder, IEnumerable<string> pinnedFolders)
    {
        if (startAtLastLocation && _folderBrowser.DirectoryExists(lastBrowsedFolder))
        {
            return lastBrowsedFolder;
        }

        return pinnedFolders.FirstOrDefault(_folderBrowser.DirectoryExists) ?? "";
    }

    public FolderNavigationResult LoadDrivesView()
    {
        return new FolderNavigationResult(
            Success: true,
            FullPath: "",
            Entries: GetDrives());
    }

    public FolderNavigationResult NavigateToPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LoadDrivesView();
        }

        if (!_folderBrowser.DirectoryExists(path))
        {
            return new FolderNavigationResult(
                Success: false,
                FullPath: path,
                Entries: [],
                ErrorMessage: $"Could not open '{path}'.");
        }

        var folders = new List<string> { GlobalConstants.ParentPathItem };
        folders.AddRange(_folderBrowser.GetSubFoldersSafe(path));

        return new FolderNavigationResult(
            Success: true,
            FullPath: path,
            Entries: folders);
    }

    public FolderNavigationResult NavigateParent(string currentPath)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return LoadDrivesView();
        }

        var parentPath = _folderBrowser.GetParent(currentPath);
        return string.IsNullOrWhiteSpace(parentPath)
            ? LoadDrivesView()
            : NavigateToPath(parentPath);
    }

    public IReadOnlyList<string> ApplyFilter(IEnumerable<string> source, string filterText)
    {
        var filter = filterText?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(filter)
            ? source.ToList()
            : source.Where(x => x.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public string BuildChildPath(string currentPath, string selectedFolder)
    {
        return string.IsNullOrWhiteSpace(currentPath)
            ? selectedFolder
            : Path.Combine(currentPath, selectedFolder);
    }

    public bool DirectoryExists(string path)
    {
        return _folderBrowser.DirectoryExists(path);
    }
}
