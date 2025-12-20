namespace OpenSkillSharp.Util;

public static class EnumerableExtensions
{
    /// <summary>
    /// Compares the length of two lists.
    /// </summary>
    /// <param name="source">Source list.</param>
    /// <param name="target">Target list.</param>
    /// <returns>Whether the given lists are of equal length.</returns>
    public static bool IsEqualLengthTo<T, K>(this IEnumerable<T> source, IEnumerable<K> target)
    {
        return source.Count() == target.Count();
    }

    /// <summary>
    /// Normalizes a list of numbers to a given range.
    /// </summary>
    /// <param name="source">A list of numbers.</param>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <returns>A list of numbers normalized to the given range.</returns>
    public static IList<double> Normalize(this IList<double> source, double min, double max)
    {
        if (source.Count == 1)
        {
            return new List<double> { max };
        }

        double srcMin = source.Min();
        double srcRange = source.Max() - srcMin;

        if (srcRange == 0)
        {
            srcRange = 0.0001;
        }

        return source.Select(v => ((v - srcMin) / srcRange * (max - min)) + min).ToList();
    }

    /// <summary>
    /// Retain the stochastic tenet of a sort to revert the original sort order.
    /// </summary>
    /// <param name="tenet">A list of tenets for each object in the target list.</param>
    /// <param name="target">A list of objects to sort.</param>
    /// <typeparam name="T">Type of object to sort.</typeparam>
    /// <returns>Ordered objects and their tenets.</returns>
    public static (IList<T> target, IList<double> tenet) Unwind<T>(this IList<double> tenet, IList<T> target)
    {
        if (tenet.Count == 0 || target.Count == 0 || tenet.Count != target.Count)
        {
            return (new List<T>(), new List<double>());
        }

        List<(double Tenet, int Index, T Object)> matrix = target
            .Select((t, i) => (Tenet: tenet[i], Index: i, Object: t))
            .OrderBy(x => x.Tenet)
            .ToList();

        List<T> sortedTarget = matrix.Select(x => x.Object).ToList();
        List<double> sortedTenet = matrix.Select(x => (double)x.Index).ToList();

        return (sortedTarget, sortedTenet);
    }
}