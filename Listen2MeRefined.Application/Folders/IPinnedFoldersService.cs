namespace Listen2MeRefined.Application.Folders;

/// <summary>
/// Provides normalization and update operations for pinned folder collections.
/// </summary>
public interface IPinnedFoldersService
{
    /// <summary>Normalizes pinned folder paths by trimming and deduplicating.</summary>
    IReadOnlyList<string> Normalize(IEnumerable<string> folders);
    /// <summary>Normalizes pinned folders and removes entries that do not exist.</summary>
    IReadOnlyList<string> NormalizeExisting(IEnumerable<string> folders);
    /// <summary>Toggles a folder in the pinned collection and returns the updated set.</summary>
    IReadOnlyList<string> TogglePinnedFolder(IEnumerable<string> currentFolders, string folderPath);
}
