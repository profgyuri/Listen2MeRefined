namespace Listen2MeRefined.Core.Interfaces;

public interface IFileAnalyzer<T>
{
    T Analyze(string path);
    IEnumerable<T> Analyze(IEnumerable<string> paths);
    Task<T> AnalyzeAsync(string path);
    Task<IEnumerable<T>> AnalyzeAsync(IEnumerable<string> paths);
}