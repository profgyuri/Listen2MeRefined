namespace Listen2MeRefined.Infrastructure;
using Ardalis.GuardClauses;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using System.Security.Cryptography;

internal static class Extensions
{
    private static readonly object _contextLock = new();

    internal static void NotExistingFile(
        this IGuardClause clause,
        string path,
        string parameterName)
    {
        if (!File.Exists(path))
        {
            throw new ArgumentException($"There is no file under this path: {path}", parameterName);
        }
    }

    internal static void AddRange<T>(
        this ICollection<T> collection,
        IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
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

    internal static void AddIfDoesNotExist<T>(
        this DataContext context,
        IEnumerable<T> items)
        where T : class
    {
        foreach (var item in items)
        {
            context.AddIfDoesNotExist(item);
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

    /// <summary>
    ///     Adds the range of items to the collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="list">The collection to add the items to.</param>
    /// <param name="items">The items to add to the collection.</param>
    public static void AddRange<T>(
        this IList<T> list,
        IList<T> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            list.Add(items[i]);
        }
    }

    /// <summary>
    ///     Removes the range of items from the collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="list">The collection to remove the items from.</param>
    /// <param name="items">The items to remove from the collection.</param>
    public static void RemoveRange<T>(
        this IList<T> list,
        IList<T> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            list.Remove(items[i]);
        }
    }

    /// <summary>
    ///     Gets a random item from the collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection.</typeparam>
    /// <param name="list">The collection to get the random item from.</param>
    /// <returns>The random item.</returns>
    public static T GetRandom<T>(this IList<T> list)
    {
        return list[RandomNumberGenerator.GetInt32(list.Count)];
    }
}