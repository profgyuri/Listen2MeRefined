namespace Listen2MeRefined.Core.Interfaces;

/// <summary>
/// Used to analyze the media files and save the metadata to the database.
/// </summary>
public interface IFolderScanner
{
    void Scan(string path);
    void Scan(IEnumerable<string> paths);
    Task ScanAsync(string path);
    Task ScanAsync(IEnumerable<string> paths);
}