namespace Listen2MeRefined.Infrastructure.Scanning.Files;

public interface IFileScanner
{
    /// <summary>
    /// Scans a single file and returns the metadata.
    /// </summary>
    /// <param name="path">The path of the file to scan.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved or updated audio metadata for the scanned file.</returns>
    Task<AudioModel> ScanAsync(string path, CancellationToken ct = default);
}
