namespace Listen2MeRefined.Infrastructure.Scanning.Folders;

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
    /// <param name="mode">Scan mode to use.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ScanAsync(string path, ScanMode mode = ScanMode.Incremental, CancellationToken ct = default);
    
    /// <summary>
    /// Creates or updates the database with the metadata of the media files,
    /// and also deletes the metadata of the files that are no longer in the folders.
    /// </summary>
    /// <param name="requests">The folder scan requests to process.</param>
    /// <param name="mode">Scan mode to use.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ScanAsync(IEnumerable<FolderScanRequest> requests, ScanMode mode = ScanMode.Incremental, CancellationToken ct = default);
    
    /// <summary>
    /// Scans all the stored folders and updates the database.
    /// </summary>
    /// <param name="mode">Scan mode to use.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ScanAllAsync(ScanMode mode = ScanMode.Incremental, CancellationToken ct = default);
}
