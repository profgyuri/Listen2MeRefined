namespace Listen2MeRefined.Infrastructure;

using Ardalis.GuardClauses;

internal static class Extensions
{
    internal static void NotExistingFile(this IGuardClause clause, string path, string parameterName)
    {
        if (!File.Exists(path))
        {
            throw new ArgumentException($"There is no file under this path: {path}", parameterName);
        }
    }
    
    internal  static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}