using System.Security.Cryptography;
using Ardalis.GuardClauses;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;

namespace Listen2MeRefined.Infrastructure.Utils;

internal static class Extensions
{
    private static readonly object _contextLock = new();

    internal static void NotExistingFile(
        this IGuardClause guardClause,
        string path,
        string parameterName)
    {
        if (!File.Exists(path))
        {
            throw new ArgumentException($"There is no file under this path: {path}", parameterName);
        }
    }

    internal static void AddIfDoesNotExist<T>(
        this DataContext context,
        T item)
        where T : class
    {
        if (!context.Set<T>().Any(x => x.Equals(item)))
        {
            context.Set<T>().Add(item);
        }
    }

    internal static async Task AddIfDoesNotExistAsync<T>(
        this DataContext context,
        IEnumerable<T> items)
        where T : class
    {
        await Task.Run(() =>
        {
            foreach (var item in items)
            {
                Monitor.Enter(_contextLock);
                try
                {
                    context.AddIfDoesNotExist(item);
                }
                finally
                {
                    Monitor.Exit(_contextLock);
                }
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    ///     Shuffles the collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="list">The collection to shuffle.</param>
    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;

        while (n > 1)
        {
            int k;
            do
            {
                k = RandomNumberGenerator.GetInt32(n);
            } while (n == k);

            n--;

            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}