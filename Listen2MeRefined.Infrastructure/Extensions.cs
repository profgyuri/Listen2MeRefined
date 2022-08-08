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
}