namespace Listen2MeRefined.Infrastructure.SystemOperations;

public interface IFileEnumerator
{
    IEnumerable<string> EnumerateFiles(string path);
    Task<IEnumerable<string>> EnumerateFilesAsync(string path);
}