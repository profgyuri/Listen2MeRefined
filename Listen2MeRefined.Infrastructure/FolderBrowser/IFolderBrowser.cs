namespace Listen2MeRefined.Infrastructure.FolderBrowser;

public interface IFolderBrowser
{
    /// <summary>
    ///     Returns every subfolder in <paramref name="path" />.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    IEnumerable<string> GetSubFolders(string path);

    /// <summary>
    ///     Returns every accessible subfolder in <paramref name="path" />.
    /// </summary>
    IEnumerable<string> GetSubFoldersSafe(string path);

    /// <summary>
    ///     Returns a list of the logical drives.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetDrives();

    /// <summary>
    ///     Checks whether the directory exists.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    ///     Returns the parent folder path, or null when no parent exists.
    /// </summary>
    string? GetParent(string path);
}
