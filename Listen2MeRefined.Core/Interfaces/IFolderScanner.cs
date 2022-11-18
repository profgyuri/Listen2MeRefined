namespace Listen2MeRefined.Core.Interfaces;

/// <summary>
/// Used to analyze the media files and save the metadata to the database.
/// </summary>
public interface IFolderScanner
{
    /// <summary>
    /// Creates or updates the database with the metadata of the media files,
    /// and also deletes the metadata of the files that are no longer in the folder.
    /// </summary>
    /// <param name="path">The path of the folder to scan.</param>
    void Scan(string path);
    
    /// <summary>
    /// Creates or updates the database with the metadata of the media files,
    /// and also deletes the metadata of the files that are no longer in the folder.
    /// </summary>
    /// <param name="paths">The paths of the folders to scan.</param>
    void Scan(IEnumerable<string> paths);
    
    /// <summary>
    /// Creates or updates the database with the metadata of the media files,
    /// and also deletes the metadata of the files that are no longer in the folder.
    /// </summary>
    /// <param name="path">The path of the folder to scan.</param>
    Task ScanAsync(string path);
    
    /// <summary>
    /// Creates or updates the database with the metadata of the media files,
    /// and also deletes the metadata of the files that are no longer in the folder.
    /// </summary>
    /// <param name="paths">The paths of the folders to scan.</param>
    Task ScanAsync(IEnumerable<string> paths);
}