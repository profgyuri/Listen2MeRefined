namespace Listen2MeRefined.Application.Files;

/// <summary>
/// Enumerates supported file paths for a folder scan.
/// </summary>
public interface IFileEnumerator
{
    /// <summary>
    /// Streams supported file paths from the target directory.
    /// </summary>
    /// <param name="path">Base directory path to enumerate.</param>
    /// <param name="includeSubdirectories">
    /// <see langword="true"/> to include descendant directories; otherwise enumerates only the top-level directory.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async stream of matching file paths.</returns>
    IAsyncEnumerable<string> EnumerateFilesAsync(
        string path,
        bool includeSubdirectories,
        CancellationToken ct = default);
}
