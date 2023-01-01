namespace Listen2MeRefined.Core.Interfaces;

public interface IMetadataExtractor<T>
{
    T Extract(string path);
    IEnumerable<T> Extract(IEnumerable<string> paths);
    Task<T> ExtractAsync(string path);
    Task<IEnumerable<T>> ExtractAsync(IEnumerable<string> paths);
}