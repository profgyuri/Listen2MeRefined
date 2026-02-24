namespace Listen2MeRefined.Infrastructure.Scanning.Files;

/// <summary>
/// Analyzes a file and produces a typed metadata model.
/// </summary>
public interface IFileAnalyzer<T>
{
    /// <summary>
    /// Analyzes a single file asynchronously.
    /// </summary>
    /// <param name="path">Absolute path of the file to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The analyzed model for the file.</returns>
    Task<T> AnalyzeAsync(string path, CancellationToken ct = default);
}
