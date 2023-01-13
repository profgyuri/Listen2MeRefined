namespace Listen2MeRefined.Core.Interfaces;

public interface IFileEnumerator
{
    IEnumerable<string> EnumerateFiles(string path);
    Task<IEnumerable<string>> EnumerateFilesAsync(string path);
}