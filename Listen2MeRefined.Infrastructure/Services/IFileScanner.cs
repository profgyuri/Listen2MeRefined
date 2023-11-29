namespace Listen2MeRefined.Infrastructure.Services;
public interface IFileScanner
{
    /// <summary>
    /// Scans a single file and returns the metadata.
    /// </summary>
    /// <param name="path">The path of the file to scan.</param>
    /// <returns></returns>
    Task<AudioModel> ScanAsync(string path);
}
