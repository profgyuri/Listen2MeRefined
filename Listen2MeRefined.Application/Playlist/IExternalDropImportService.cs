namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Imports externally dropped audio files into playlist collections.
/// </summary>
public interface IExternalDropImportService
{
    /// <summary>
    /// Handles shell-dropped files and inserts scanned songs into playlist collections.
    /// </summary>
    /// <param name="droppedPaths">The file paths dropped by the shell.</param>
    /// <param name="insertIndex">The preferred insertion index.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleExternalFileDropAsync(IReadOnlyList<string> droppedPaths, int insertIndex, CancellationToken ct = default);
}
