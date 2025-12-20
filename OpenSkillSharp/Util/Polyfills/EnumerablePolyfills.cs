#if !NET9_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Linq;

internal static class EnumerableIndexPolyfill
{
    /// <summary>Returns an enumerable that incorporates the element's index into a tuple.</summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">The source enumerable providing the elements.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    public static IEnumerable<(int Index, TSource Item)> Index<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int index = -1;

        foreach (TSource element in source)
        {
            index++;
            yield return (index, element);
        }
    }
}

#endif