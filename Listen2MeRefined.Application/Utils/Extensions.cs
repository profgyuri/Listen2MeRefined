namespace Listen2MeRefined.Application.Utils;

public static class Extensions
{
    internal static void AddRange<T>(
        this ICollection<T> collection,
        IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
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
}