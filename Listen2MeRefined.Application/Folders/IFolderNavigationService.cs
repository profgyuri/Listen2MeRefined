namespace Listen2MeRefined.Application.Folders;

/// <summary>
/// Encapsulates folder-browser navigation and filtering logic.
/// </summary>
public interface IFolderNavigationService
{
    /// <summary>Gets available drive roots for the current machine.</summary>
    IReadOnlyList<string> GetDrives();
    /// <summary>Resolves the initial path to show in the folder browser.</summary>
    string ResolveInitialPath(bool startAtLastLocation, string lastBrowsedFolder, IEnumerable<string> pinnedFolders);
    /// <summary>Builds a navigation result representing the drives view.</summary>
    FolderNavigationResult LoadDrivesView();
    /// <summary>Navigates to the provided path and returns resulting entries.</summary>
    FolderNavigationResult NavigateToPath(string path);
    /// <summary>Navigates to the parent of the current path.</summary>
    FolderNavigationResult NavigateParent(string currentPath);
    /// <summary>Applies a case-insensitive filter to source entries.</summary>
    IReadOnlyList<string> ApplyFilter(IEnumerable<string> source, string filterText);
    /// <summary>Builds a child path from current path and selected folder.</summary>
    string BuildChildPath(string currentPath, string selectedFolder);
    /// <summary>Checks whether the supplied directory path exists.</summary>
    bool DirectoryExists(string path);
}
