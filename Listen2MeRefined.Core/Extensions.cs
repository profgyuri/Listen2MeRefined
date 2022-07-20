namespace Listen2MeRefined.Core;

using System.Security.Cryptography;

public static class Extensions
{
    /// <summary>
    ///     Randomizes the order of a list.
    /// </summary>
    /// <typeparam name="T">Type of the elements in the <list type="."</typeparam>
    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;

        while (n > 1)
        {
            int k;
            do
            {
                k = RandomNumberGenerator.GetInt32(n);
            } while (n - 1 == k); //with this condition there is less chance that an entity will stay in place

            n--;

            (list[k], list[n]) = (list[n], list[k]); //switch the values at index n and k
        }
    }
}
